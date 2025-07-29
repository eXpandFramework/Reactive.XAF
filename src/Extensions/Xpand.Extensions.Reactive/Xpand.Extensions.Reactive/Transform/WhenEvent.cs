using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.TypeExtensions;
using Type = System.Type;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        private static readonly ConcurrentDictionary<(Type type, string eventName),(EventInfo info,MethodInfo add,MethodInfo remove)> Events = new();
        public static readonly IScheduler ImmediateScheduler=Scheduler.Immediate;

        public static IObservable<EventPattern<object>> WhenEvent(this object source,string eventName,IScheduler scheduler=null,[CallerMemberName]string caller="") 
            => source.FromEventPattern<EventArgs>(eventName,scheduler,caller)
                .Select(pattern => new EventPattern<object>(pattern.Sender, pattern.EventArgs));

        internal static IObservable<EventPattern<TArgs>> FromEventPattern<TArgs>(this object source, string eventName,IScheduler scheduler,[CallerMemberName]string caller="") {
            var eventInfo = source.EventInfo(eventName);
            IObservable<EventPattern<TArgs>> eventStream;

            if ((eventInfo.info.EventHandlerType?.IsGenericType ?? false) && eventInfo.info.EventHandlerType.GenericTypeArguments.First() == typeof(TArgs)) {
                eventStream = Observable.FromEventPattern<TArgs>(
                        handler => eventInfo.add.Invoke(source, [handler]),
                        handler => eventInfo.remove.Invoke(source, [handler]), scheduler ?? ImmediateScheduler)
                    .Select(pattern => new EventPattern<TArgs>(pattern.Sender, pattern.EventArgs));
            }
            else if (eventInfo.add.IsPublic && !eventInfo.add.IsStatic) {
                eventStream = Observable.FromEventPattern<TArgs>(source, eventName, scheduler ?? ImmediateScheduler);
            }
            else if (eventInfo.info.EventHandlerType == typeof(EventHandler)) {
                eventStream = Observable.FromEventPattern(
                        handler => eventInfo.add.Invoke(source, [handler]),
                        handler => eventInfo.remove.Invoke(source, [handler]), scheduler ?? ImmediateScheduler)
                    .Select(pattern => new EventPattern<TArgs>(pattern.Sender, (TArgs)pattern.EventArgs));
            }
            else {
                eventStream = Observable.FromEventPattern<TArgs>(
                        handler => eventInfo.add.Invoke(source, [handler]),
                        handler => eventInfo.remove.Invoke(source, [handler]), scheduler ?? ImmediateScheduler)
                    .Select(pattern => new EventPattern<TArgs>(pattern.Sender, pattern.EventArgs));
            }

            return eventStream
                .TakeUntilDisposed(source as IComponent, caller)
                ; 
        }
        
        private static (EventInfo info,MethodInfo add,MethodInfo remove) EventInfo(this object source,string eventName) 
            => Events.GetOrAdd((source as Type ?? source.GetType(), eventName), t => {
                var eventInfo = (EventInfo)t.type.Members(MemberTypes.Event,Flags.AllMembers).OrderByDescending(info => info.IsPublic())
                    .First(info => info.Name == eventName || info.Name.EndsWith(".".JoinString(eventName)));
                return (eventInfo, eventInfo.AddMethod,eventInfo.RemoveMethod);
            });

        public static IObservable<(TEventArgs args, TSource source)> WhenEvent<TSource,TEventArgs>(this object source, string eventName,IScheduler scheduler=null,[CallerMemberName]string caller="")
            => source.FromEventPattern<TEventArgs>(eventName,scheduler,caller).Select(pattern => (pattern.EventArgs,(TSource)source));
        
        public static IObservable<TEventArgs> WhenEvent<TEventArgs>(this object source, string eventName,IScheduler scheduler=null,[CallerMemberName]string caller="") 
            => source.FromEventPattern<TEventArgs>(eventName,scheduler,caller).Select(pattern => pattern.EventArgs) ;
        public static IObservable<TResult> WhenEvent<TEventArgs,TResult>(this object source, string eventName,Func<TEventArgs,IObservable<TResult>> selector,IScheduler scheduler=null,[CallerMemberName]string caller="") 
            => source.FromEventPattern<TEventArgs>(eventName,scheduler,caller).SelectMany(pattern => selector(pattern.EventArgs)) ;
        
        
    }
}