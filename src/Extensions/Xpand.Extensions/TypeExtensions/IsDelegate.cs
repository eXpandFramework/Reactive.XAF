using System;

namespace Xpand.Extensions.TypeExtensions {
    public static partial class TypeExtensions {
        public static bool IsDelegate(this Type type)
            => typeof(Delegate).IsAssignableFrom(type);
    }
}