using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Source.Extensions.Linq{
    public static partial class Extensions{
        public static IEnumerable<T> GetItems<T>(this IEnumerable collection,
            Func<T, IEnumerable> selector) {
            var stack = new Stack<IEnumerable<T>>();
            stack.Push(collection.OfType<T>());

            while (stack.Count > 0) {
                IEnumerable<T> items = stack.Pop();
                foreach (var item in items) {
                    yield return item;

                    IEnumerable<T> children = selector(item).OfType<T>();
                    stack.Push(children);
                }
            }
        }

    }
}
