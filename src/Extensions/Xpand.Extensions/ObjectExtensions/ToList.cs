using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static IList<T> ToList<T>(this object obj)
            => obj is IList<T> list ? list : obj.ToEnumerable<T>().ToList();
    }
}