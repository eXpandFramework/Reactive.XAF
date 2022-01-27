using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.TypeExtensions;

namespace Xpand.Extensions.Reactive.Create {
    public static partial class Create {
        public static IObservable<T> RepeatDefaultValueDuringSilence<T>(this IObservable<T> source,
            TimeSpan maxQuietPeriod, IScheduler scheduler = null)
            => source.RepeatDuringSilence(maxQuietPeriod, _ => typeof(T).DefaultValue().ReturnObservable().Cast<T>(),scheduler);
    }
}