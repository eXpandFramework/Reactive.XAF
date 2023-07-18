using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Conditional{
    public static partial class Conditional {
        public static IObservable<T> TakeUntilDisposed<T>(this IObservable<T> source, IComponent component, [CallerMemberName] string caller = "")
            => component != null ? source.TakeUntil(component.WhenDisposed(caller)) : source;
    }
}