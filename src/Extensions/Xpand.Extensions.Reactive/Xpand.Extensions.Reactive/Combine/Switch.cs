using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Xpand.Extensions.Reactive.Combine{
    public static partial class Combine{
        public static IObservable<T> SwitchIfEmpty<T>(this IObservable<T> source, IObservable<T> switchTo) 
            => Observable.Using(() => new AsyncSubject<Unit>(), signal => source.Do(_ => {
                signal.OnNext(Unit.Default);
                signal.OnCompleted();
            }).Concat(switchTo.TakeUntil(signal)));

        public static IObservable<T> SwitchIfDefault<T>(this IObservable<T> source, IObservable<T> switchTo)  
            => source.Select(entry => !EqualityComparer<T>.Default.Equals(entry, default) ? Observable.Return(entry) : switchTo)
                .TakeUntil(stream => stream == switchTo)
                .Concat();
    }
}