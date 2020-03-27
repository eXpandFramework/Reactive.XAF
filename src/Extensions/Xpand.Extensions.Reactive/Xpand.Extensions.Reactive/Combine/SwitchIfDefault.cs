using System;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Combine{
    public static partial class Combine{
        public static IObservable<T> SwitchIfDefault<T>(this IObservable<T> @this, IObservable<T> switchTo)
            where T : class{
            if (@this == null) throw new ArgumentNullException(nameof(@this));
            if (switchTo == null) throw new ArgumentNullException(nameof(switchTo));
            return @this.SelectMany(entry => entry != default(T) ? entry.ReturnObservable<T>() : switchTo);
        }
    }
}