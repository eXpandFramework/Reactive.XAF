using System;
using System.Linq;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<TSource[]> BufferUntilCompleted<TSource>(this IObservable<TSource> source,bool skipEmpty=false){
            var allEvents = source.Publish().RefCount();
            return allEvents.Buffer(allEvents.LastAsync()).Select(list => list.ToArray()).Where(sources => !skipEmpty||sources.Any());
        }
    }
}