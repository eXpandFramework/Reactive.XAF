using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> Defer<T>(this object o, IObservable<T> execute)
            => Observable.Defer(() => execute);
        public static IObservable<T> Defer<T>(this object o, Func<IObservable<T>> selector)
            => Observable.Defer(selector);
        public static IObservable<T> Defer<T>(this object o, Func<IEnumerable<T>> selector)
            => Observable.Defer(() => selector().ToNowObservable());

        public static IObservable<T> Defer<T>(this object o, Action execute)
            => Observable.Defer(() => {
                execute();
                return Unit.Default.ReturnObservable();
            }).To<T>();
        
        public static IObservable<Unit> Defer(this object o, Action execute)
            => Observable.Defer(() => {
                execute();
                return Unit.Default.ReturnObservable();
            });
    }
}