using System;
using System.Collections;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static bool IsEnumerable(this object o) 
            => o is IEnumerable && o switch {
                string => false,
                Type type => TypeExtensions.TypeExtensions.IsEnumerable(type),
                _ => true
            };
    }
}