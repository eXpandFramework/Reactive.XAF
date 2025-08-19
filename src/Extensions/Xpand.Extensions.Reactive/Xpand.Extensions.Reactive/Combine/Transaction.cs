using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Combine {
    public static partial class Combine {
        public static IObservable<Unit> SequentialTransaction(this IEnumerable<object> source, bool failFast=false,
            Func<IObservable<object>, IObservable<object>> resiliencePolicy=null, object[] context = null, [CallerMemberName]string transactionName = null) 
            => source.ToObjectStreams().SequentialTransaction(failFast, resiliencePolicy, context, transactionName);
        



        public static IObservable<Unit> SequentialTransaction<TSource>(this IEnumerable<IObservable<TSource>> source, bool failFast=false,
            Func<IObservable<TSource>, IObservable<TSource>> resiliencePolicy=null, object[] context=null, [CallerMemberName] string transactionName = null) {
    


            var transaction = source.Operations( resiliencePolicy, context, failFast, transactionName)
                .SequentialTransaction(context,transactionName.PrefixCallerWhenDefault());

            var result = failFast
                ? transaction.Catch((Exception ex) => Observable.Throw<Unit>(new InvalidOperationException($"{transactionName} failed", ex)))
                : transaction;

            return result;
        }
        
        public static IObservable<TSource[]> SequentialTransactionWithResults<TSource>(this IEnumerable<IObservable<TSource>> source,
            Func<IObservable<TSource>, IObservable<TSource>> resiliencePolicy = null, object[] context = null,
            [CallerMemberName] string transactionName = null)
            => source.Select((obs, i) => (resiliencePolicy != null ? resiliencePolicy(obs) : obs)
                    .ChainFaultContext((context ?? []).AddToArray($"{transactionName} - Op:{i + 1}"))
                    .Select(item => (object)item)
                    .Catch((FaultHubException ex) => Observable.Return((object)ex)))
                .ToNowObservable().Concat().ToList()
                .SelectMany(results => {
                    var exceptions = results.OfType<Exception>().ToList();
                    return !exceptions.Any() ? Observable.Return(results.Cast<TSource>().ToArray())
                        : Observable.Throw<TSource[]>(new InvalidOperationException($"{transactionName} failed",
                            new AggregateException(exceptions)));
                })
                .ChainFaultContext(context.AddToContext(transactionName));


        static IObservable<Unit> SequentialTransaction(this IEnumerable<IObservable<object>> source,object[] context, string transactionName ) 
            => source.ToNowObservable().SelectManySequential(obs => obs.DefaultIfEmpty(new object())).BufferUntilCompleted()
                .Select(results => results.OfType<Exception>().ToList())
                .SelectMany(allFailures => !allFailures.Any()
                    ? Unit.Default.Observe()
                    : Observable.Throw<Unit>(new AggregateException(allFailures)))
                .PushStackFrame(context.AddToContext(transactionName));
        private static IEnumerable<IObservable<object>> Operations<TSource>(this IEnumerable<IObservable<TSource>> source, Func<IObservable<TSource>, IObservable<TSource>> resiliencePolicy, object[] context, bool failFast, string transactionName) 
            => source.Select((obs, i) => (resiliencePolicy?.Invoke(obs)??obs)
                    .ChainFaultContext((context??[]).AddToArray($"{transactionName} - Op:{i + 1}"))
                    .Select(t => (object)t))
                .Select(operation =>  failFast ? operation : operation.Catch((FaultHubException ex) => Observable.Return<object>(ex)));
        
    }
}