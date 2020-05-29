using System;

namespace Xpand.Extensions.ReflectionExtensions{
    public static partial class ReflectionExtensions{
        public static bool IsNullableType(this Type theType) => theType.IsGenericType && theType.GetGenericTypeDefinition() == typeof(Nullable<>);
    }
}