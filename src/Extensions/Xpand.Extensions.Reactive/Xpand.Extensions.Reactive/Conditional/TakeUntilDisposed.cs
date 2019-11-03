using System;
using System.ComponentModel;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Conditional{
    public static class Conditional{
        public static IObservable<T> TakeUntilDisposed<T>(this IObservable<T> source, IComponent component){
            return source.TakeUntil(component.WhenDisposed());
        }
    }
}