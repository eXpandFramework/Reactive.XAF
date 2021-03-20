using System;
using System.Linq;
using Xpand.Extensions.ReflectionExtensions;

namespace Xpand.Extensions.TypeExtensions {
    public static partial class TypeExtensions {
        public static Type RealType(this Type type) 
            => ((type.IsEnumerable()) && type.IsGenericType) || type.IsNullableType() ? 
                type.GenericTypeArguments.First() : type.IsArray ? type.GetElementType() : type;
    }
}