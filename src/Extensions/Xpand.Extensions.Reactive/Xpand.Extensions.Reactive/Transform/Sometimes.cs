using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<T> Sometimes<T>(this IObservable<T> source,Func<IObservable<T>,IObservable<T>> target, IObservable<bool> isOn)
            => source.Publish(obs => target(obs).Publish(sampled => isOn.Select(b => b ? sampled : obs).Switch()));
    }
}