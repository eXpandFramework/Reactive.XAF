using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Combine {
    public static partial class Combine {
        static IObservable<Unit> ExecuteTransaction(this IEnumerable<IObservable<object>> source,object[] context, string transactionName = "Transaction") 
            => source.ToNowObservable().SelectManySequential(obs => obs.DefaultIfEmpty(new object())).BufferUntilCompleted()
                .Select(results => results.OfType<Exception>().ToList())
                .SelectMany(allFailures => !allFailures.Any() ? Unit.Default.Observe() : Observable.Throw<Unit>(new InvalidOperationException($"{transactionName} failed", new AggregateException(allFailures))))
                .ChainFaultContext(context.AddToContext(transactionName));

        public static IObservable<Unit> ExecuteTransaction(this IEnumerable<object> source,Func<IObservable<object>, IObservable<object>> resiliencePolicy,bool failFast=false, string transactionName = "Transaction") 
            => source.ExecuteTransaction(resiliencePolicy,[],failFast,transactionName);
        
        public static IObservable<Unit> ExecuteTransaction(this IEnumerable<object> source,Func<IObservable<object>, IObservable<object>> resiliencePolicy,object[] context,bool failFast=false, string transactionName = "Transaction") 
            => source.ToObjectStreams().ExecuteTransaction(resiliencePolicy,context,failFast, transactionName);

        public static IObservable<Unit> ExecuteTransaction<TSource>(this IEnumerable<IObservable<TSource>> source,
            Func<IObservable<TSource>, IObservable<TSource>> resiliencePolicy,bool failFast=false, string transactionName = "Transaction")
            => source.ExecuteTransaction(resiliencePolicy,[],failFast,transactionName);

        public static IObservable<Unit> ExecuteTransaction<TSource>(this IEnumerable<IObservable<TSource>> source,
            Func<IObservable<TSource>, IObservable<TSource>> resiliencePolicy, object[] context, bool failFast = false,
            string transactionName = "Transaction") {
            var transaction = source.Operations( resiliencePolicy, context, failFast, transactionName)
                .ExecuteTransaction(context,transactionName);
            return !failFast ? transaction : transaction.Catch((Exception ex)
                        => Observable.Throw<Unit>(new InvalidOperationException($"{transactionName} failed", ex)))
                    .ChainFaultContext(context.AddToContext(transactionName));
        }

        private static IEnumerable<IObservable<object>> Operations<TSource>(this IEnumerable<IObservable<TSource>> source, Func<IObservable<TSource>, IObservable<TSource>> resiliencePolicy, object[] context, bool failFast, string transactionName) 
            => source.Select((obs, i) => resiliencePolicy(obs)
                    .ChainFaultContext(context.AddToArray($"{transactionName} - Op:{i + 1}"))
                    .Select(t => (object)t))
                .Select(operation =>  failFast ? operation : operation.Catch((FaultHubException ex) => Observable.Return<object>(ex)));
        
    }
}