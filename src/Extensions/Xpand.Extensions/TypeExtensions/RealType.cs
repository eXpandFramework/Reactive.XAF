using System;
using System.Linq;
using Xpand.Extensions.ReflectionExtensions;

namespace Xpand.Extensions.TypeExtensions {
    public static partial class TypeExtensions {
        public static Type RealType(this Type type) {
            if ((type.IsEnumerable() && type.IsGenericType) || type.IsNullableType())
                return type.GenericTypeArguments.First();
            else if (type.IsArray)
                return type.GetElementType();
            else
                return type;
        }
    }
}