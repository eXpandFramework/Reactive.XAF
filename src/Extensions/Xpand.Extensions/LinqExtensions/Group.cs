using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static IGrouping<TK, TV> AsGroup<TK, TV>(this IEnumerable<TV> source, TK key) 
            => Create(key, source);

        private static IGrouping<TK, TV> Create<TK, TV>(TK key, IEnumerable<TV> source) 
            => new SimpleGroupWrapper<TK, TV>(key, source);

        internal class SimpleGroupWrapper<TK, TV>(TK key, IEnumerable<TV> source) : IGrouping<TK, TV> {
            private readonly IEnumerable<TV> _source = source ?? throw new NullReferenceException("source");

            public TK Key{ get; } = key;

            public IEnumerator<TV> GetEnumerator() => _source.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _source.GetEnumerator();
        }
        
        public static Dictionary<int, List<T>> GroupUntilNext<T>(this IEnumerable<T> items, Func<T, string> methodSelector) {
            var result = new Dictionary<int, List<T>>();
            var groupIndex = 0;
            var groupItems = new List<T>();
            string currentMethod = null;
            foreach (var item in items) {
                var itemMethod = methodSelector(item);
                if (groupItems.Count == 0) {
                    groupItems.Add(item);
                    currentMethod = itemMethod;
                }
                else if (itemMethod == currentMethod) {
                    result[groupIndex] = groupItems;
                    groupIndex++;
                    groupItems = [item];
                }
                else {
                    groupItems.Add(item);
                }
            }
            if (groupItems.Count > 0) result[groupIndex] = groupItems;
            return result.Reverse().Select((x, i) => new { Key = i, x.Value }).ToDictionary(x => x.Key, x => x.Value);
        }

    }
}