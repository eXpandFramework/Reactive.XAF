using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.TypeExtensions {
    public static partial class TypeExtensions {
        public static bool IsEnumerable(this Type type)
            => type != typeof(String) && typeof(IEnumerable).IsAssignableFrom(type) ||
               (type == typeof(IEnumerable) || (type?.GetInterfaces().OfType<IEnumerable>().Any() ?? false));

    }


    
 
}