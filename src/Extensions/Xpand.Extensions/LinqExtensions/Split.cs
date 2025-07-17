using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions{
    public static partial class LinqExtensions{
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> list, int parts){
            var i = 0;
            return list.GroupBy(_ => i++ % parts).Select(part => part.AsEnumerable());
        }
        
        public static IEnumerable<string> TrimAll(this IEnumerable<string> source)
            => source.Select(s => s?.Trim());

    }
}