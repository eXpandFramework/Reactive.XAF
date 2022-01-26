using System;
using System.Reactive.Concurrency;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Create;

public static partial class Create {
    public static IObservable<T[]> RepeatEmptyDuringSilence<T>(this IObservable<T[]> source, TimeSpan maxQuietPeriod, IScheduler scheduler = null)
        => source.RepeatDuringSilence(maxQuietPeriod, _ => Array.Empty<T>().ReturnObservable());
}