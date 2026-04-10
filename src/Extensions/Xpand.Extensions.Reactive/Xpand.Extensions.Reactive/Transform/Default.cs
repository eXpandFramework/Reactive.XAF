using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<T> NonDefaultIfEmpty<T>(
            this IObservable<T> source,
            T nonDefaultValue,
            bool filterSourceDefaults = false) {
            var stream = source;

            if (filterSourceDefaults) {
                // Optional: Removes default(T) from the stream (e.g., removes 01/01/0001)
                stream = source.Where(x => !Object.Equals(x, default(T)));
            }

            return stream.DefaultIfEmpty(nonDefaultValue);
        }
    }
}