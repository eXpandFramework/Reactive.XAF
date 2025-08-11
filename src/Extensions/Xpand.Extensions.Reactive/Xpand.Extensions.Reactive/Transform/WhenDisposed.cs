using System;
using System.ComponentModel;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<TDisposable> Disposed<TDisposable>(this IObservable<TDisposable> source) where TDisposable:IComponent 
            => source.SelectMany(item => item.WhenDisposed());

        public static IObservable<TDisposable> WhenDisposed<TDisposable>(this TDisposable source) where TDisposable : IComponent 
            => Observable.FromEventPattern(source, nameof(source.Disposed), ImmediateScheduler).Take(1).To(source);
    }
}