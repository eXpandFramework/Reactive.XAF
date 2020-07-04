using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<T2> SelectManySequential<T1, T2>(this IObservable<T1> source, Func<T1, IObservable<T2>> selector) => 
            source.Select(x => Observable.Defer(() => selector(x))).Concat();
    }
}