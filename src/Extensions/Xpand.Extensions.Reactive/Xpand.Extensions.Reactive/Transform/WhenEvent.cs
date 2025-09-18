﻿using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.TypeExtensions;
using Type = System.Type;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        private static readonly ConcurrentDictionary<(Type type, string eventName),(EventInfo info,MethodInfo add,MethodInfo remove)> Events = new();
        public static readonly IScheduler ImmediateScheduler=Scheduler.Immediate;

        [Obsolete("use "+nameof(ProcessEvent))]
        public static IObservable<EventPattern<object>> WhenEvent(this object source,string eventName,IScheduler scheduler=null) 
            => source.FromEventPattern<EventArgs>(eventName,scheduler)
                .Select(pattern => new EventPattern<object>(pattern.Sender, pattern.EventArgs));
        
        public static IObservable<TEventArgs> ProcessEvent<TEventArgs>(this object source, string eventName, IScheduler scheduler = null) where TEventArgs:EventArgs
            => source.FromEventPattern<TEventArgs>(eventName, scheduler).Select(pattern => pattern.EventArgs);
        
        public static IObservable<T> ProcessEvent<T>(this T source, string eventName, IScheduler scheduler = null) 
            => source.FromEventPattern<EventArgs>(eventName, scheduler).Select(pattern => pattern.EventArgs).To(source);

        public static IObservable<EventPattern<TArgs>> FromEventPattern<TArgs>(this object source, string eventName, IScheduler scheduler) {
            var eventInfo = source.EventInfo(eventName);
            if (eventInfo.info == null) {
                throw new ArgumentException($"Event '{eventName}' not found on type '{source.GetType().Name}'.");
            }

            var observable = Observable.Create<EventPattern<TArgs>>(observer => {
                var eventHandlerType = eventInfo.info.EventHandlerType;

                var delegateParams = eventHandlerType!.GetMethod("Invoke")!.GetParameters();
                var handlerParams = delegateParams.Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();

                Expression senderExpression = Expression.Constant(source);
                Expression argsExpression = null;

                if (delegateParams.Length == 1) {
                    argsExpression = handlerParams[0];
                }
                else if (delegateParams.Length == 2 && delegateParams[0].ParameterType == typeof(object)) {
                    senderExpression = handlerParams[0];
                    argsExpression = handlerParams[1];
                }
                else if (delegateParams.Length == 0 && typeof(TArgs) == typeof(Unit)) {
                    argsExpression = Expression.Constant(Unit.Default);
                }

                if (argsExpression == null) {
                    observer.OnError(new NotSupportedException($"Unsupported event signature for '{eventName}'."));
                    return Disposable.Empty;
                }

                var onNextMethod = typeof(IObserver<EventPattern<TArgs>>).GetMethod("OnNext");
                var eventPatternCtor = typeof(EventPattern<TArgs>).GetConstructor([typeof(object), typeof(TArgs)]);
                var newEventPattern = Expression.New(eventPatternCtor!, senderExpression,
                    Expression.Convert(argsExpression, typeof(TArgs)));
                var onNextCall = Expression.Call(Expression.Constant(observer), onNextMethod!, newEventPattern);

                var handler = Expression.Lambda(eventHandlerType, onNextCall, handlerParams).Compile();

                eventInfo.add.Invoke(source, [handler]);
                return Disposable.Create(eventInfo.remove,info => info.Invoke(source,[handler]));
            });

            if (scheduler != null) {
                observable = observable.ObserveOn(scheduler);
            }

            return observable.TakeUntilDisposed(source as IComponent);
        }

        private static (EventInfo info,MethodInfo add,MethodInfo remove) EventInfo(this object source,string eventName) 
            => Events.GetOrAdd((source as Type ?? source.GetType(), eventName), t => {
                var eventInfo = (EventInfo)t.type.Members(MemberTypes.Event,Flags.AllMembers).OrderByDescending(info => info.IsPublic())
                    .First(info => info.Name == eventName || info.Name.EndsWith(".".JoinString(eventName)),() => $"Event '{eventName}' not found on type '{source.GetType().FullName}'.");
                return (eventInfo, eventInfo.AddMethod,eventInfo.RemoveMethod);
            });

        public static IObservable<(TEventArgs args, TSource source)> WhenEvent<TSource,TEventArgs>(this object source, string eventName,IScheduler scheduler=null)  
            => source.FromEventPattern<TEventArgs>(eventName,scheduler).Select(pattern => (pattern.EventArgs,(TSource)source));
        
        [Obsolete("use "+nameof(ProcessEvent))]
        public static IObservable<TEventArgs> WhenEvent<TEventArgs>(this object source, string eventName,IScheduler scheduler=null)  
            => source.FromEventPattern<TEventArgs>(eventName,scheduler).Select(pattern => pattern.EventArgs) ;
    }
}