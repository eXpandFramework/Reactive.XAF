using System;
using System.Linq;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<(T item, int length)> CountSubsequent<T>(this IObservable<T> source, Func<T, object> key){
            var eventTraceSubject = source.Publish().RefCount();
            return eventTraceSubject
                .GroupByUntil(key, _ => eventTraceSubject.DistinctUntilChanged(key))
                .SelectMany(ob => {
                    var r = ob.Replay();
                    r.Connect();
                    return r.IgnoreElements().Concat(default(T).Observe())
                        .Select(_ => r.ToEnumerable().ToArray());
                })
                .Select(_ => (_.First(), _.Length));
        }

	    public static IObservable<int> CountLiveStreams<T>(this IObservable<IObservable<T>> streamOfStreams) 
            => streamOfStreams.Select(x => x.IgnoreElements().Select(_ => 0).Catch(Observable.Empty<int>()).Prepend(1).Append(-1))
                .Merge()
                .Scan(0, (accumulator, delta) => accumulator + delta)
                .Prepend(0);
        
        public static IObservable<int> LiveCount<T>(this IObservable<T> source)
            => source.Select((_, b) => b + 1);

        public static IObservable<long> LiveLongCount<T>(this IObservable<T> source) 
            => source.Scan(0L, (a, _) => a + 1);
        
        
    }
}