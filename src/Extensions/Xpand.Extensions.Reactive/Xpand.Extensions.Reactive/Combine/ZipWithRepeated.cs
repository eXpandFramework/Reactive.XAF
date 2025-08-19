using System;
using System.Linq;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Combine {
    public static partial class Combine {
        
        
        
        
        public static IObservable<(TFirst First, TSecond Second)> ZipWithRepeated<TFirst, TSecond>(
            this IObservable<TFirst> source, IObservable<TSecond> other) 
            => other.Replay(replayed => source.ToAsyncEnumerable()
                .Zip(replayed.ToAsyncEnumerable().Repeat())
                .ToObservable());
    }
}