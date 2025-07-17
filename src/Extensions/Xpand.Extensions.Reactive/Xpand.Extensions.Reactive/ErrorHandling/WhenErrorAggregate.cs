using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.ErrorHandling {
    public static partial class ErrorHandling {
        public static IObservable<T> WhenErrorAggregate<T>(this IObservable<T> source, [CallerMemberName] string caller = "")
            => source
                // .Catch<T, Exception>(exception => new AggregateException(caller, exception).Throw<T>())
            ;
    }
}