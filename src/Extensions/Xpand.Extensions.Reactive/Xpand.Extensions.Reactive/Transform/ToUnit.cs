using System;
using System.Reactive;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<Unit> ToUnit<T>(this IObservable<T> source) => source.Select(_ => Unit.Default);
    }
}