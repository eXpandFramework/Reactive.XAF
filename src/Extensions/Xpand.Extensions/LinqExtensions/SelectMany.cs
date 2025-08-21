using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static IEnumerable<object> ManySelect(this IEnumerable<object> source)
            => source.SelectMany(o => o as IEnumerable<object> ?? o.YieldItem());
        
        public static IEnumerable<TSource> SelectMany<TSource>(this IEnumerable<IEnumerable<TSource>> source) 
            => source.SelectMany(sources => sources);
        public static TSource[] SelectArray<TSource>(this IEnumerable<IEnumerable<TSource>> source) 
            => source.SelectMany().ToArray();
        
        
    }
}