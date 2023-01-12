using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<Unit> ToUnit<T>(this IObservable<T> source) => source.Select(_ => Unit.Default);
        public static IEnumerable<Unit> ToUnit<T>(this IEnumerable<T> source) => source.Select(_ => Unit.Default);
        
    }
}