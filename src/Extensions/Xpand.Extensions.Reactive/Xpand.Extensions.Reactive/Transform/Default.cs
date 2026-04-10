using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<T> NonDefaultIfEmpty<T>(this IObservable<T> source, T nonDefaultValue,
            bool filterSourceDefaults = false) {
            var stream = source;
            if (filterSourceDefaults) {
                stream = source.Where(x => !Equals(x, default(T)));
            }

            return stream.DefaultIfEmpty(nonDefaultValue);
        }
    }
}