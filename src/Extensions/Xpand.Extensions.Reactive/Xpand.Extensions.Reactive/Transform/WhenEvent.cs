
using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static readonly IScheduler ImmediateScheduler=Scheduler.Immediate;

        public static IObservable<EventPattern<object>> WhenEvent(this object source,params string[] eventNames)
            => eventNames.ToNowObservable().SelectMany(source.FromEventPattern<EventArgs>)
                .Select(pattern => new EventPattern<object>(pattern.Sender,pattern.EventArgs));

        private static IObservable<EventPattern<TArgs>> FromEventPattern<TArgs>(this object source, string eventName) {
            if (source is Type type) {
                var eventInfo = type.GetEvent(eventName) ?? throw new ArgumentNullException($"{eventName}");
                return Observable.FromEventPattern(handler => eventInfo.AddEventHandler(null, handler),
                    handler => eventInfo.RemoveEventHandler(null, handler)
                ).Select(pattern => new EventPattern<TArgs>(pattern.Sender, (TArgs)pattern.EventArgs));
            }
            return Observable.FromEventPattern(source, eventName, ImmediateScheduler)
                .Select(pattern => new EventPattern<TArgs>(pattern.Sender, (TArgs)pattern.EventArgs));
        }

        public static IObservable<(TEventArgs args, TSource source)> WhenEvent<TSource,TEventArgs>(this object source, params string[] eventNames)
            => eventNames.ToNowObservable().SelectMany(eventName => source.FromEventPattern<TEventArgs>(eventName).Select(pattern => (pattern.EventArgs,(TSource)source)));
        
        public static IObservable<TEventArgs> WhenEvent<TEventArgs>(this object source, params string[] eventNames)
            => eventNames.ToNowObservable().SelectMany(eventName => source.FromEventPattern<TEventArgs>(eventName).Select(pattern => pattern.EventArgs));
    }
}