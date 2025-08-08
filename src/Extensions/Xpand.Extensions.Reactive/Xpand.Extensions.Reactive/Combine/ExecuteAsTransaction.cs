using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Combine {
    public static partial class Combine {
        public static IObservable<Unit> ExecuteTransaction(this IEnumerable<IObservable<object>> source, string transactionName = "Transaction") 
            => source.ToNowObservable()
                .SelectManySequential(obs => obs.DefaultIfEmpty(true).Select(value => value is not false))
                .BufferUntilCompleted()
                .SelectMany(results => results.All(r => r) ? Unit.Default.Observe()
                    : Observable.Throw<Unit>(new InvalidOperationException($"{transactionName} failed")));    
        public static IObservable<Unit> ExecuteTransaction(this IEnumerable<IObservable<Unit>> source, string transactionName = "Transaction") 
            => source.Select(s => s.ToTransactional()).ExecuteTransaction(transactionName);
        
        public static IObservable<object> ToTransactional<TSource>(this IObservable<TSource> source)
            => source.Select(t => (object)t).Catch((FaultHubException _) => Observable.Return<object>(false));

    }
}