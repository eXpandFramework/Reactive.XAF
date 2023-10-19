using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.Combine{
    public static partial class Combine{
        public static IObservable<T> SwitchIfEmpty<T>(this IObservable<T> source, IObservable<T> switchTo) 
            => new AsyncSubject<Unit>().Use(signal => source.Do(_ => {
                signal.OnNext(Unit.Default); 
                signal.OnCompleted();
            }).Concat(switchTo.TakeUntil(signal)));
    }
}