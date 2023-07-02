using System;
using System.Collections.Generic;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions { 
        public static IEnumerable<TU> Scan<T, TU>(this IEnumerable<T> input, Func<TU, T, TU> next, TU state) {
            yield return state;
            foreach (var item in input) {
                state = next(state, item);
                yield return state;
            }
        }
    }
}