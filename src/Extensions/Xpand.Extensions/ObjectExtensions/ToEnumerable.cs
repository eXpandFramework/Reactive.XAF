using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static IEnumerable<object> ToEnumerable(this object obj) 
            => obj.ToEnumerable<object>();

        public static IEnumerable<T> ToEnumerable<T>(this object obj) 
            => obj is string || !(obj is IEnumerable enumerable) ? new[] {obj}.Cast<T>() : enumerable.Cast<T>();
    }
}