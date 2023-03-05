using System;
using System.Collections.Generic;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static IEnumerable<T> Traverse<T>(this T root, Func<T, IEnumerable<T>> children, bool includeSelf = true) {
            if (includeSelf)
                yield return root;
            var stack = new Stack<IEnumerator<T>>();
            try {
                stack.Push(children(root).GetEnumerator());
                while (stack.Count != 0) {
                    var enumerator = stack.Peek();
                    if (!enumerator.MoveNext()) {
                        stack.Pop();
                        enumerator.Dispose();
                    }
                    else {
                        yield return enumerator.Current;
                        stack.Push(children(enumerator.Current).GetEnumerator());
                    }
                }
            }
            finally {
                foreach (var enumerator in stack)
                    enumerator.Dispose();
            }
        }
    }
}