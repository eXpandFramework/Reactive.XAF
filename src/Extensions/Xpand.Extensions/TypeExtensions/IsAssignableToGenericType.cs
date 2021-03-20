using System;
using System.Linq;

namespace Xpand.Extensions.TypeExtensions {
    public static partial class TypeExtensions {
        public static bool IsAssignableToGenericType(this Type givenType, Type genericType)
            => givenType.GetInterfaces().Any(it => it.IsGenericType && it.GetGenericTypeDefinition() == genericType) ||
               (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType ||
                givenType.BaseType != null && IsAssignableToGenericType(givenType.BaseType, genericType));
    }
}