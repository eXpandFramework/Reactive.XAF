using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;
using Type = System.Type;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        private static readonly ConcurrentDictionary<(Type type, string eventName),(EventInfo info,MethodInfo add,MethodInfo remove)> Events = new();
        public static readonly IScheduler ImmediateScheduler=Scheduler.Immediate;

        public static IObservable<EventPattern<object>> WhenEvent(this object source,params string[] eventNames) 
            => eventNames.ToNowObservable().SelectMany(source.FromEventPattern<EventArgs>)
                .Select(pattern => new EventPattern<object>(pattern.Sender, pattern.EventArgs));
        
        private static IObservable<EventPattern<TArgs>> FromEventPattern<TArgs>(this object source, string eventName) {
            var eventInfo = source.EventInfo(eventName);
            return eventInfo.info.EventHandlerType?.IsGenericType ?? false
                ? Observable.FromEventPattern<TArgs>(handler => eventInfo.add.Invoke(source, new object[] { handler }),
                        handler => eventInfo.remove.Invoke(source, new object[] { handler }))
                    .Select(pattern => new EventPattern<TArgs>(pattern.Sender, pattern.EventArgs))
                : Observable.FromEventPattern(handler => eventInfo.info.AddEventHandler(source, handler),
                        handler => eventInfo.info.RemoveEventHandler(source, handler))
                    .Select(pattern => new EventPattern<TArgs>(pattern.Sender, (TArgs)pattern.EventArgs));
        }
        
        private static (EventInfo info,MethodInfo add,MethodInfo remove) EventInfo(this object source,string eventName) 
            => Events.GetOrAdd((source as Type ?? source.GetType(), eventName), t => {
                var eventInfo = (EventInfo)t.type.Members(MemberTypes.Event,Flags.AllMembers).First(info =>
                    info.Name == eventName || info.Name.EndsWith(".".JoinString(eventName)));
                return (eventInfo, eventInfo.AddMethod,eventInfo.RemoveMethod);
            });

        public static IObservable<(TEventArgs args, TSource source)> WhenEvent<TSource,TEventArgs>(this object source, params string[] eventNames)
            => eventNames.ToNowObservable().SelectMany(eventName => source.FromEventPattern<TEventArgs>(eventName).Select(pattern => (pattern.EventArgs,(TSource)source)));
        
        public static IObservable<TEventArgs> WhenEvent<TEventArgs>(this object source, params string[] eventNames) 
            => eventNames.ToNowObservable().SelectMany(eventName =>
                source.FromEventPattern<TEventArgs>(eventName).Select(pattern => pattern.EventArgs));
    }
}