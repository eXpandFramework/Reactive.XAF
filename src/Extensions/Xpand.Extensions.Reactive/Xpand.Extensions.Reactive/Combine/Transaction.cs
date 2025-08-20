using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        public static IObservable<T> ConcurrentTransaction<T>(this IEnumerable<IObservable<T>> source,
            string transactionName, bool failFast = false, int maxConcurrency = 0, object[] context = null, IScheduler scheduler = null)
            => Observable.Defer(() => {
                    var scheduledSource = source.ToObservable(scheduler ?? Scheduler.Default);
                    return failFast ? scheduledSource.ConcurrentFailFast(transactionName, maxConcurrency, context)
                        : scheduledSource.ConcurrentRunToEnd(transactionName, maxConcurrency, context);
                })
                .ChainFaultContext(context, null, transactionName);

        private static IObservable<T> ConcurrentRunToEnd<T>(this IObservable<IObservable<T>> source,string transactionName, int maxConcurrency, object[] context) 
            => source.Select((obs, i) => obs
                    .ChainFaultContext(context.AddToContext($"{transactionName} - Op:{i + 1}"))
                    .Materialize())
                .Merge(maxConcurrency > 0 ? maxConcurrency : int.MaxValue)
                .ToList()
                .SelectMany(notifications => {
                    var exceptions = notifications.Where(n => n.Kind == NotificationKind.OnError).Select(n => n.Exception).ToList();
                    var results = notifications.Where(n => n.Kind == NotificationKind.OnNext).Select(n => n.Value).ToList();
                    return exceptions.Any() ? results.ToObservable().Concat(Observable.Throw<T>(new AggregateException(exceptions))) : results.ToObservable();
                });

        private static IObservable<T> ConcurrentFailFast<T>(this IObservable<IObservable<T>> source,string transactionName, int maxConcurrency, object[] context) 
            => source.Select((obs, i) => obs.PushStackFrame(context.AddToContext($"{transactionName} - Op:{i + 1}")))
                .Merge(maxConcurrency > 0 ? maxConcurrency : int.MaxValue);

        public static IObservable<Unit> SequentialTransaction(this IEnumerable<object> source, bool failFast = false,
            Func<IObservable<object>, IObservable<object>> resiliencePolicy = null, object[] context = null,
            [CallerMemberName] string transactionName = null, IScheduler scheduler = null)
            => source.ToObjectStreams()
                .SequentialTransaction(failFast, resiliencePolicy, context, transactionName, scheduler);

        public static IObservable<Unit> SequentialTransaction<TSource>(this IEnumerable<IObservable<TSource>> source,
            bool failFast = false,
            Func<IObservable<TSource>, IObservable<TSource>> resiliencePolicy = null, object[] context = null,
            [CallerMemberName] string transactionName = null, IScheduler scheduler = null) {
            var transaction = source
                .Operations(resiliencePolicy, context, failFast, transactionName, scheduler: scheduler)
                .SequentialTransaction(context.AddToContext(transactionName.PrefixCallerWhenDefault()));
            return failFast
                ? transaction.Catch((Exception ex)
                    => Observable.Throw<Unit>(new InvalidOperationException($"{transactionName} failed", ex)))
                : transaction;
        }

        public static IObservable<TResult> SequentialTransaction<TSource, TResult>(
            this IEnumerable<IObservable<TSource>> source, Func<TSource[], IObservable<TResult>> resultSelector,
            Func<IObservable<TSource>, IObservable<TSource>> resiliencePolicy = null, object[] context = null,
            [CallerMemberName] string transactionName = null, IScheduler scheduler = null)
            => source.Select((obs, i) => {
                    var operation = resiliencePolicy != null ? resiliencePolicy(obs) : obs;
                    return operation.SubscribeOn(scheduler ?? Scheduler.Default)
                        .ChainFaultContext((context ?? []).AddToArray($"{transactionName} - Op:{i + 1}"))
                        .Select(item => (object)item)
                        .Catch((FaultHubException ex) => Observable.Return((object)ex));
                })
                .ToNowObservable().Concat().ToList()
                .SelectMany(results => {
                    var exceptions = results.OfType<Exception>().ToList();
                    return !exceptions.Any()
                        ? Observable.Return(results.Cast<TSource>().ToArray())
                        : Observable.Throw<TSource[]>(new InvalidOperationException($"{transactionName} failed",
                            new AggregateException(exceptions)));
                })
                .SelectMany(resultSelector)
                .PushStackFrame(context.AddToContext(transactionName));

        static IObservable<Unit> SequentialTransaction(this IEnumerable<IObservable<object>> source, object[] context)
            => source.ToNowObservable().SelectManySequential(obs => obs.DefaultIfEmpty(new object()))
                .BufferUntilCompleted()
                .Select(results => results.OfType<Exception>().ToList())
                .SelectMany(allFailures => !allFailures.Any()
                    ? Unit.Default.Observe()
                    : Observable.Throw<Unit>(new AggregateException(allFailures)))
                .PushStackFrame(context);

        private static IEnumerable<IObservable<object>> Operations<TSource>(this IEnumerable<IObservable<TSource>> source,
            Func<IObservable<TSource>, IObservable<TSource>> resiliencePolicy, object[] context, bool failFast,
            string transactionName, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0, IScheduler scheduler = null)
            => source.Select((obs, i) => (resiliencePolicy?.Invoke(obs) ?? obs)
                    .SubscribeOn(scheduler ?? Scheduler.Default)
                    .ChainFaultContext(context.AddToContext($"{transactionName} - Op:{i + 1}"), null, memberName, filePath, lineNumber)
                    .Select(t => (object)t))
                .Select(operation => failFast ? operation : operation.Catch((FaultHubException ex) => Observable.Return<object>(ex)));
        
        [SuppressMessage("ReSharper", "UnusedTypeParameter")]
        public interface ITransactionBuilder<out TCurrentResult> { }

        internal class TransactionBuilder<TCurrentResult>(IObservable<object> initialStep, string transactionName, object[] context, IScheduler scheduler,
            List<Func<object, IObservable<object>>> subsequentSteps) : ITransactionBuilder<TCurrentResult> {
            internal readonly IObservable<object> InitialStep = initialStep;
            internal readonly List<Func<object, IObservable<object>>> SubsequentSteps = subsequentSteps;
            internal readonly string TransactionName = transactionName;
            internal readonly object[] Context = context;
            internal readonly IScheduler Scheduler = scheduler;
            internal string CallerMemberName;
            internal string CallerMemberPath;
            internal int CallerMemberLine;
            public TransactionBuilder(IObservable<object> initialStep, string transactionName, object[] context,
                IScheduler scheduler, string callerMemberName,string callerMemberPath,int callerMemberLine) : this(initialStep, transactionName, context, scheduler, new List<Func<object, IObservable<object>>>()) {
                CallerMemberName = callerMemberName;
                CallerMemberPath=callerMemberPath;
                CallerMemberLine = callerMemberLine;
            }
        }

        public static ITransactionBuilder<IList<TSource>> BeginTransaction<TSource>(this IObservable<TSource> source, string transactionName=null, object[] context = null,
            IScheduler scheduler = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => new TransactionBuilder<IList<TSource>>(source.BufferUntilCompleted().Select(list => (object)list), transactionName??memberName, context, scheduler, memberName, filePath, lineNumber);


        public static ITransactionBuilder<TNext> Then<TCurrent, TNext>(
            this ITransactionBuilder<TCurrent> builder, Func<TCurrent, IObservable<TNext>> nextSelector) {
            var ib = (TransactionBuilder<TCurrent>)builder;
            return new TransactionBuilder<TNext>(ib.InitialStep, ib.TransactionName, ib.Context, ib.Scheduler,
                [..ib.SubsequentSteps, currentResult => nextSelector((TCurrent)currentResult).Select(nextResult => (object)nextResult)]) {
                CallerMemberName = ib.CallerMemberName,CallerMemberPath = ib.CallerMemberPath,CallerMemberLine = ib.CallerMemberLine
            };
        }
        
        public static IObservable<TFinal> RunFailFast<TFinal>(this ITransactionBuilder<TFinal> builder)
            => builder.Run();

        public static IObservable<TFinal> RunToEnd<TFinal>(this ITransactionBuilder<TFinal> builder)
            => builder.Run(false);

        private static IObservable<TFinal> Run<TFinal>(this ITransactionBuilder<TFinal> builder, bool failFast = true) {
            var ib = (TransactionBuilder<TFinal>)builder;
            var allSteps = new List<Func<object, IObservable<object>>> { _ => ib.InitialStep };
            allSteps.AddRange(ib.SubsequentSteps);
            var transactionLogic = failFast ? ib.FailFast(allSteps) : ib.RunToCompletion( allSteps);
            var finalLogic = transactionLogic.Select(result => (TFinal)result);
            var scheduledTransaction = ib.Scheduler == null ? finalLogic : finalLogic.SubscribeOn(ib.Scheduler);
            return scheduledTransaction.ChainFaultContext(ib.Context, null, ib.CallerMemberName, ib.CallerMemberPath, ib.CallerMemberLine);
        }

        private static IObservable<object> RunToCompletion<TFinal>(this TransactionBuilder<TFinal> builder, List<Func<object, IObservable<object>>> allSteps) {
            var allFailures = new List<Exception>();
            var stepChain = Observable.Return((object)null);
            for (var i = 0; i < allSteps.Count; i++) {
                var step = allSteps[i];
                var partNumber = i + 1;
                stepChain = stepChain.SelectMany(currentResult => Observable.Defer(() => step(currentResult))
                    .PushStackFrame(builder.Context.AddToContext($"{builder.TransactionName} - Part {partNumber}",builder.CallerMemberName,builder.CallerMemberPath,builder.CallerMemberLine))
                    .Materialize()
                    .Select(notification => {
                        if (notification.Kind != NotificationKind.OnError) return notification.Value;
                        allFailures.Add(notification.Exception.ExceptionToPublish(FaultHub.LogicalStackContext.Value.NewFaultContext([])));
                        return null;
                    }));
            }

            return stepChain.SelectMany(lastResult => allFailures.Any() ? Observable.Throw<object>(new AggregateException(allFailures)) : Observable.Return(lastResult));
        }
        private static IObservable<object> FailFast<TFinal>(this TransactionBuilder<TFinal> builder,List<Func<object, IObservable<object>>> allSteps) 
            => allSteps.Select((step, i) => new { Func = step, PartNumber = i + 1 }).Aggregate(Observable.Return((object)null), (currentObservable, stepInfo) => currentObservable
                .SelectMany(currentResult => Observable.Defer(() => stepInfo.Func(currentResult))
                    .PushStackFrame(builder.Context.AddToContext($"{builder.TransactionName} - Part {stepInfo.PartNumber}"))));

    }
}