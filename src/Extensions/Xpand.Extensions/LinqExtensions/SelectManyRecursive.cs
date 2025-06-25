using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions{
    public static partial class LinqExtensions{

        public static IEnumerable<T> SelectManyRecursive<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> childrenSelector){
            foreach (var i in source){
                yield return i;
                var children = childrenSelector(i);
                if (children == null) continue;
                foreach (var child in SelectManyRecursive(children, childrenSelector))
                    yield return child;
            }
        }

        [Obsolete("use "+nameof(SelectManyRecursive))]
        public static IEnumerable<T> GetItems<T>(this IEnumerable collection, Func<T, IEnumerable> selector, Func<T, object> distinctSelector = null) {
            var hashSet = distinctSelector != null ? new HashSet<object>() : null;
            var stack = new Stack<IEnumerable<T>>([collection.OfType<T>()]);
            while (stack.TryPop(out var items))
                foreach (var item in items) {
                    var key = distinctSelector?.Invoke(item);
                    if (hashSet != null && !hashSet.Add(key)) continue;
                    yield return item;
                    stack.Push(selector(item).OfType<T>());
                }
        }
    }
}
