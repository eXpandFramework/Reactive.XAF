using System;
using System.Reactive;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<EventPattern<object>> WhenEvent(this object source, string eventName)
            => Observable.FromEventPattern(source, eventName);
        
        public static IObservable<(TEventArgs args, TSource source)> WhenEvent<TSource,TEventArgs>(this object source, string eventName)
            => Observable.FromEventPattern(source, eventName).Select(pattern => (((TEventArgs) pattern.EventArgs),(TSource)source));
        
        public static IObservable<TEventArgs> WhenEvent<TEventArgs>(this object source, string eventName)
            => Observable.FromEventPattern(source, eventName).Select(pattern => ((TEventArgs) pattern.EventArgs));
    }
}