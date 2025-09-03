﻿using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Combine{
    public static partial class Combine{
        public static IObservable<T> SwitchIfEmpty<T>(this IObservable<T> source, IObservable<T> switchTo) 
            => Observable.Using(() => new AsyncSubject<Unit>(), signal => source.Do(_ => {
                signal.OnNext(Unit.Default);
                signal.OnCompleted();
            }).Concat(switchTo.TakeUntil(signal)));

        public static IObservable<T> SwitchIfDefault<T>(this IObservable<T> @this, IObservable<T> switchTo) where T : class 
            => @this.SelectMany(entry => entry != null ? entry.Observe() : switchTo);
    }
}