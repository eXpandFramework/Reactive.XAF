using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<TSource> DoWhen<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate, Action<TSource> action)
            => source.Do(source1 => {
                if (predicate(source1)) {
                    action(source1);
                }
            });
        public static IObservable<TSource> DoWhen<TSource>(this IObservable<TSource> source, Func<int,TSource, bool> predicate, Action<TSource> action)
            => source.Select((source1, i) => {
                if (predicate(i,source1)) {
                        action(source1);
                }
                return source1;
            });
        public static IObservable<TSource> DoWhen<TSource>(this IObservable<TSource> source, Func<int,TSource, bool> predicate, Action<TSource,int> action)
            => source.Select((source1, i) => {
                if (predicate(i,source1)) {
                        action(source1,i);
                }
                return source1;
            });
    }
}