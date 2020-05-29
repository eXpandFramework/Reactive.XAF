using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static T Wait<T>(this IObservable<T> source, TimeSpan timeSpan) => source.Timeout(timeSpan).Wait();
    }
}