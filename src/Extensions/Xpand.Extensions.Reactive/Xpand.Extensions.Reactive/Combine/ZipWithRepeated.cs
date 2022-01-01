using System;
using System.Linq;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Combine;

public static partial class Combine {
    /// <summary>
    /// starts receiving merged pairs as soon as both sequences have emitted a value, not when the source2 sequence has completed. the elements of the source2 sequence are buffered two times. One time by the Replay operator, and another one by the replayed.ToAsyncEnumerable()
    /// </summary>
    /// <returns></returns>
    public static IObservable<(TFirst First, TSecond Second)> ZipWithRepeated<TFirst, TSecond>(
        this IObservable<TFirst> source, IObservable<TSecond> other) 
        => other.Replay(replayed => source.ToAsyncEnumerable()
            .Zip(replayed.ToAsyncEnumerable().Repeat())
            .ToObservable());
}