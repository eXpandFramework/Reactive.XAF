using System.Collections.Generic;
using System.Linq;

namespace Xpand.Source.Extensions.Linq{
    internal static partial class LinqExtensions{
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> list, int parts){
            var i = 0;
            return list.GroupBy(item => i++ % parts).Select(part => part.AsEnumerable());
        }
    }
}