using System;
using System.Diagnostics;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> WhenDebuggerNotAttached<T>(this IObservable<T> source)
            => source.Where(_ => !Debugger.IsAttached);
        public static IObservable<T> WhenDebuggerAttached<T>(this IObservable<T> source)
            => source.Where(_ => !Debugger.IsAttached);
    }
}