using System;
using System.Linq;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform{

    public static partial class Transform{
        public static IObservable<(T item, int length)> CountSubsequent<T>(this IObservable<T> source,
            Func<T, object> key){
            var eventTraceSubject = source.Publish().RefCount();
            return eventTraceSubject
                .GroupByUntil(key, _ => eventTraceSubject.DistinctUntilChanged(key))
                .SelectMany(ob => {
                    var r = ob.Replay();
                    r.Connect();
                    return r.IgnoreElements().Concat(default(T).ReturnObservable())
                        .Select(_ => r.ToEnumerable().ToArray());
                })
                .Select(_ => {
                    var item = _.First();

                    return (item, _.Length);
                });
        }
    }
}