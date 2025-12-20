using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;

namespace Xpand.Extensions.Reactive.Combine {
    public static partial class Combine {
        
        
        
        
        public static IObservable<(TFirst First, TSecond Second)> ZipWithRepeated<TFirst, TSecond>(
            this IObservable<TFirst> source, IObservable<TSecond> other) 
            => Observable.Throw<(TFirst First, TSecond Second)>(new AbandonedMutexException());
            // => other.Replay(replayed => source.ToAsyncEnumerable()
            //     .Zip(replayed.ToAsyncEnumerable().Repeat())
            //     .ToObservable());
    }
}