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

        public static IObservable<EventPattern<object>> WhenEvent(this object source,string eventName,[CallerMemberName]string caller="") 
            => source.FromEventPattern<EventArgs>(eventName,caller)
                .Select(pattern => new EventPattern<object>(pattern.Sender, pattern.EventArgs));

        private static IObservable<EventPattern<TArgs>> FromEventPattern<TArgs>(this object source, string eventName,[CallerMemberName]string caller="") {
            var eventInfo = source.EventInfo(eventName);
            if ((eventInfo.info.EventHandlerType?.IsGenericType ?? false)&&eventInfo.info.EventHandlerType.GenericTypeArguments.First()==typeof(TArgs)) {
                return Observable.FromEventPattern<TArgs>(
                        handler => eventInfo.add.Invoke(source, new object[] { handler }),
                        handler => eventInfo.remove.Invoke(source, new object[] { handler }),ImmediateScheduler)
                    .Select(pattern => new EventPattern<TArgs>(pattern.Sender, pattern.EventArgs))
                    .TakeUntilDisposed(source as IComponent,caller)
                    ;
            }

            if (eventInfo.add.IsPublic&&!eventInfo.add.IsStatic) {
                return Observable.FromEventPattern<TArgs>(source, eventName,ImmediateScheduler)
                    .TakeUntilDisposed(source as IComponent,caller)
                    ;    
            }

            if (eventInfo.info.EventHandlerType == typeof(EventHandler)) {
                return Observable.FromEventPattern(
                        handler => eventInfo.add.Invoke(source, [handler]),
                        handler => eventInfo.remove.Invoke(source, [handler]),ImmediateScheduler)
                    .Select(pattern => new EventPattern<TArgs>(pattern.Sender, (TArgs)pattern.EventArgs))
                    .TakeUntilDisposed(source as IComponent,caller)
                    ;    
            }
            return Observable.FromEventPattern<TArgs>(
                    handler => eventInfo.add.Invoke(source, [handler]),
                    handler => eventInfo.remove.Invoke(source, [handler]),ImmediateScheduler)
                .Select(pattern => new EventPattern<TArgs>(pattern.Sender, pattern.EventArgs))
                .TakeUntilDisposed(source as IComponent,caller)
                ;
        }
        
        private static (EventInfo info,MethodInfo add,MethodInfo remove) EventInfo(this object source,string eventName) 
            => Events.GetOrAdd((source as Type ?? source.GetType(), eventName), t => {
                var eventInfo = (EventInfo)t.type.Members(MemberTypes.Event,Flags.AllMembers).OrderByDescending(info => info.IsPublic())
                    .First(info => info.Name == eventName || info.Name.EndsWith(".".JoinString(eventName)));
                return (eventInfo, eventInfo.AddMethod,eventInfo.RemoveMethod);
            });

        public static IObservable<(TEventArgs args, TSource source)> WhenEvent<TSource,TEventArgs>(this object source, string eventName,[CallerMemberName]string caller="")
            => source.FromEventPattern<TEventArgs>(eventName,caller).Select(pattern => (pattern.EventArgs,(TSource)source));
        
        public static IObservable<TEventArgs> WhenEvent<TEventArgs>(this object source, string eventName,[CallerMemberName]string caller="") 
            => source.FromEventPattern<TEventArgs>(eventName,caller).Select(pattern => pattern.EventArgs);
    }
}