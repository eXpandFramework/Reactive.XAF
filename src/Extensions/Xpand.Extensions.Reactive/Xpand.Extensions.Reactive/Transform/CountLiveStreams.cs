using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
	    public static IObservable<int> CountLiveStreams<T>(this IObservable<IObservable<T>> streamOfStreams) 
            => streamOfStreams.Select(x => x.IgnoreElements().Select(_ => 0).Catch(Observable.Empty<int>()).Prepend(1).Append(-1))
                .Merge()
                .Scan(0, (accumulator, delta) => accumulator + delta)
                .Prepend(0);
    }
}