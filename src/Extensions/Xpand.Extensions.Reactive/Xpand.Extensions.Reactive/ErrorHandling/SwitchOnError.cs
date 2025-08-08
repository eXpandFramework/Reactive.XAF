using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.ErrorHandling {
    public static partial class ErrorHandling {
        public static IObservable<TResult> SwitchOnError<TSource, TResult>(this IObservable<TSource> source, Func<FaultHubException, IObservable<TResult>> fallbackSelector, 
            Func<IObservable<TSource>, IObservable<TSource>> retryStrategy = null, object[] context = null, [CallerMemberName] string caller = "")
            => source.ChainFaultContext(retryStrategy, context, caller)
                .Select(t => (TResult)(object)t).Catch(fallbackSelector);

        public static IObservable<Unit> SwitchOnError<TSource>(this IObservable<TSource> source, 
            Func<IObservable<TSource>, IObservable<TSource>> retryStrategy = null, object[] context = null, [CallerMemberName] string caller = "")
            => source.SwitchOnError(_ => Unit.Default.Observe(),retryStrategy, context, caller);
    }
}