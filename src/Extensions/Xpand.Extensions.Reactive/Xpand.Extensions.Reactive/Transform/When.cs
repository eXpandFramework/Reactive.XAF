﻿using System;
using System.Reactive;
using System.Reactive.Linq;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Filter;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<Notification<T>> When<T>(this IObservable<T> source, NotificationKind kind) 
            => source.Materialize().SelectMany(n => n.Kind == NotificationKind.OnError 
                ? n.Exception.Throw<Notification<T>>() : n.Kind == kind ? Observable.Return(n) : Observable.Empty<Notification<T>>());

        public static IObservable<T[]> WhenCompleted<T>(this IObservable<T> source) 
            // => source.When(NotificationKind.OnCompleted).Select(_ => Array.Empty<T>());
            => source.BufferUntilCompleted();

        public static IObservable<Exception> WhenError<T>(this IObservable<T> source) 
            => source.When(NotificationKind.OnError).Select(notification => notification.Exception);
        
        public static IObservable<T[]> WhenFinished<T>(this IObservable<T> source) 
            => source.Publish(obs => obs.WhenCompleted().Merge(obs.WhenError().Select(_ => Array.Empty<T>())).Take(1));
        
        public static IObservable<T> WhenIs<T>(this object source)
            => source.As<T>().Observe().WhenNotDefault();
        
        public static IObservable<T> WhenIs<T>(this T source,string typeName)
            => source.As(typeName).Observe().WhenNotDefault();
    }
}