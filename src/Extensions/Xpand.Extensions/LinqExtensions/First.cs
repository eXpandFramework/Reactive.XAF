using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static T First<T>(this IEnumerable<T> source, Func<string> exceptionMessage)
            => source.FirstOrDefault() ?? throw new InvalidOperationException(exceptionMessage());
        public static T First<T>(this IEnumerable<T> source,Func<T,bool> predicate, Func<string> exceptionMessage)
            => source.FirstOrDefault(predicate) ?? throw new InvalidOperationException(exceptionMessage());
    }
}