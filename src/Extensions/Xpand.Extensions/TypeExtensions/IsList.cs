using System;
using System.Collections.Generic;

namespace Xpand.Extensions.TypeExtensions {
    public static partial class TypeExtensions {
        public static bool IsList(this Type type)
            => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
    }
}