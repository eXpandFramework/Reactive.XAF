using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Combine {
    
    public static partial class Combine {
        public static IObservable<Unit> SequentialTransaction(this IEnumerable<object> source, bool failFast=false,
            Func<IObservable<object>, IObservable<object>> resiliencePolicy=null, object[] context = null, [CallerMemberName]string transactionName = null,IScheduler scheduler=null) 
            => source.ToObjectStreams().SequentialTransaction(failFast, resiliencePolicy, context, transactionName,scheduler);

        public static IObservable<Unit> SequentialTransaction<TSource>(this IEnumerable<IObservable<TSource>> source, bool failFast=false,
            Func<IObservable<TSource>, IObservable<TSource>> resiliencePolicy=null, object[] context=null, [CallerMemberName] string transactionName = null,IScheduler scheduler=null) {
            var transaction = source.Operations( resiliencePolicy, context, failFast, transactionName,scheduler:scheduler)
                .SequentialTransaction(context.AddToContext(transactionName.PrefixCallerWhenDefault()));
            return failFast ? transaction.Catch((Exception ex) => Observable.Throw<Unit>(new InvalidOperationException($"{transactionName} failed", ex))) : transaction;
        }
        
        public static IObservable<TResult> SequentialTransaction<TSource,TResult>(this IEnumerable<IObservable<TSource>> source, Func<TSource[],IObservable<TResult>> resultSelector,
            Func<IObservable<TSource>, IObservable<TSource>> resiliencePolicy = null, object[] context = null, [CallerMemberName] string transactionName = null,IScheduler scheduler=null)
            => source.Select((obs, i) => {
                    var operation = resiliencePolicy != null ? resiliencePolicy(obs) : obs;
                    return operation.SubscribeOn(scheduler??Scheduler.Default)
                        .ChainFaultContext((context ?? []).AddToArray($"{transactionName} - Op:{i + 1}"))
                        .Select(item => (object)item)
                        .Catch((FaultHubException ex) => Observable.Return((object)ex));
                })
                .ToNowObservable().Concat().ToList()
                .SelectMany(results => {
                    var exceptions = results.OfType<Exception>().ToList();
                    return !exceptions.Any() ? Observable.Return(results.Cast<TSource>().ToArray())
                        : Observable.Throw<TSource[]>(new InvalidOperationException($"{transactionName} failed",
                            new AggregateException(exceptions)));
                })
                .SelectMany(resultSelector)
                .PushStackFrame(context.AddToContext(transactionName));
        
        static IObservable<Unit> SequentialTransaction(this IEnumerable<IObservable<object>> source,object[] context ) 
            => source.ToNowObservable().SelectManySequential(obs => obs.DefaultIfEmpty(new object())).BufferUntilCompleted()
                .Select(results => results.OfType<Exception>().ToList())
                .SelectMany(allFailures => !allFailures.Any() ? Unit.Default.Observe()
                    : Observable.Throw<Unit>(new AggregateException(allFailures)))
                .PushStackFrame(context);

        private static IEnumerable<IObservable<object>> Operations<TSource>(this IEnumerable<IObservable<TSource>> source,
            Func<IObservable<TSource>, IObservable<TSource>> resiliencePolicy, object[] context, bool failFast, string transactionName,
            [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0,IScheduler scheduler=null) 
            => source.Select((obs, i) => (resiliencePolicy?.Invoke(obs)??obs).SubscribeOn(scheduler??Scheduler.Default)
                    .ChainFaultContext(context.AddToContext($"{transactionName} - Op:{i + 1}"),null,memberName, filePath, lineNumber)
                    .Select(t => (object)t))
                .Select(operation =>  failFast ? operation : operation.Catch((FaultHubException ex) => Observable.Return<object>(ex)));

        
        public static IObservable<TSecond> SequentialTransaction<TFirst, TSecond>(this IObservable<TFirst> source,
            Func<IList<TFirst>, IObservable<TSecond>> secondStreamSelector, bool failFast = true, object[] context = null,
            [CallerMemberName] string transactionName = null, IScheduler scheduler = null) {
            var transactionLogic = Observable.Defer(() => {
                var firstPart = source
                    .PushStackFrame(context.AddToContext($"{transactionName} - Part 1"))
                    .BufferUntilCompleted() ;

                if (!failFast) firstPart = firstPart.ContinueOnFault(context:context.AddToContext(transactionName)).DefaultIfEmpty([]);

                return firstPart.SelectMany(results => Observable.Defer(() => secondStreamSelector(results))
                    .PushStackFrame(context.AddToContext($"{transactionName} - Part 2")));
            });

            var scheduledTransaction = scheduler == null ? transactionLogic : transactionLogic.SubscribeOn(scheduler);

            return scheduledTransaction.ChainFaultContext(context, null, transactionName);
        }

        public interface ITransactionBuilder<out TCurrentResult> { }

        internal class TransactionBuilder<TCurrentResult>(
            IObservable<object> previousStep,
            string transactionName,
            object[] context,
            IScheduler scheduler,
            int partCounter)
            : ITransactionBuilder<TCurrentResult> {
            internal readonly IObservable<object> PreviousStep = previousStep;
            internal readonly string TransactionName = transactionName;
            internal readonly object[] Context = context;
            internal readonly IScheduler Scheduler = scheduler;
            internal readonly int PartCounter = partCounter;
        }

        public static ITransactionBuilder<IList<TSource>> BeginTransaction<TSource>(this IObservable<TSource> source, string transactionName ,
            object[] context = null, IScheduler scheduler = null) {
            var firstPart = source
                .PushStackFrame(context.AddToContext($"{transactionName} - Part 1"))
                .BufferUntilCompleted()
                .Select(list => (object)list);

            return new TransactionBuilder<IList<TSource>>(firstPart, transactionName, context, scheduler, 1);
        }

        public static ITransactionBuilder<TNext> Then<TCurrent, TNext>(
            this ITransactionBuilder<TCurrent> builder,
            Func<TCurrent, IObservable<TNext>> nextSelector) {
            var internalBuilder = (TransactionBuilder<TCurrent>)builder;
            var currentPartNumber = internalBuilder.PartCounter + 1;

            var nextStep = internalBuilder.PreviousStep.SelectMany(currentResult =>
                Observable.Defer(() => nextSelector((TCurrent)currentResult))
                    .PushStackFrame(
                        internalBuilder.Context.AddToContext(
                            $"{internalBuilder.TransactionName} - Part {currentPartNumber}"))
            ).Select(nextResult => (object)nextResult);

            return new TransactionBuilder<TNext>(nextStep, internalBuilder.TransactionName, internalBuilder.Context,
                internalBuilder.Scheduler, currentPartNumber);
        }

        public static IObservable<TFinal> Run<TFinal>(this ITransactionBuilder<TFinal> builder, bool failFast = true) {
            if (!failFast)
                throw new NotImplementedException(
                    "The 'failFast = false' (run-to-completion) mode is not yet implemented for the fluent transaction builder.");

            var internalBuilder = (TransactionBuilder<TFinal>)builder;
            var finalLogic = internalBuilder.PreviousStep.Select(result => (TFinal)result);

            var scheduledTransaction = internalBuilder.Scheduler == null
                ? finalLogic
                : finalLogic.SubscribeOn(internalBuilder.Scheduler);

            return scheduledTransaction.ChainFaultContext(internalBuilder.Context, null,
                internalBuilder.TransactionName);
        }
        
        public static IObservable<TThird> SequentialTransaction<TFirst, TSecond, TThird>(this IObservable<TFirst> source,
            Func<IList<TFirst>, IObservable<TSecond>> secondStreamSelector,
            Func<TSecond, IObservable<TThird>> thirdStreamSelector,
            bool failFast = true, object[] context = null,
            [CallerMemberName] string transactionName = null, IScheduler scheduler = null) {
            var transactionLogic = Observable.Defer(() => {
                var firstPart = source
                    .PushStackFrame(context.AddToContext($"{transactionName} - Part 1"))
                    .BufferUntilCompleted();

                if (!failFast) {
                    throw new NotImplementedException("The 'failFast = false' (run-to-completion) mode is not yet implemented for this overload.");
                }

                return firstPart
                    .SelectMany(results => Observable.Defer(() => secondStreamSelector(results))
                        .PushStackFrame(context.AddToContext($"{transactionName} - Part 2"))
                    )
                    .SelectMany(secondResult => Observable.Defer(() => thirdStreamSelector(secondResult))
                        .PushStackFrame(context.AddToContext($"{transactionName} - Part 3"))
                    );
            });
	        
            var scheduledTransaction = scheduler == null ? transactionLogic : transactionLogic.SubscribeOn(scheduler);
	        
            return scheduledTransaction.ChainFaultContext(context, null, transactionName);
        }
    }
}