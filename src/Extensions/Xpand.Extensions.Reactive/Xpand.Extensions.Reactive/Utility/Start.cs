using System;
using System.Reactive;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> StartSequential<T>(this IObservable<T> source)
            => source.SelectManySequential(arg => Observable.Start(() => arg));
        
        public static IObservable<T> Start<T>(this IObservable<T> source)
            => source.SelectMany(arg => Observable.Start(() => arg));
        
        public static IObservable<T> StartWith<T>(this IObservable<Unit> source,T value) 
            => Observable.StartWith(source,Unit.Default).To(value);
        
        public static IObservable<T> StartWith<T>(this IObservable<T> source,Func<bool> when,params T[] values) 
            => when() ? source.StartWith(values) : source;
    }
}