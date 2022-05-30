using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static IEnumerable<(TSource source, TValue other)> Pair<TSource, TValue>(
            this IEnumerable<TSource> source, TValue value)
            => source.Select(_ => (_, value));
        
        public static IEnumerable<(TSource source, TValue other)> Pairs<TSource, TValue>(
            this Dictionary<TSource,TValue> source)
            => source.Select(_ => (_.Key, _.Value));
    }
}