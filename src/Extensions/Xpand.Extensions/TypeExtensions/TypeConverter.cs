using System;
using System.ComponentModel;

namespace Xpand.Extensions.TypeExtensions {
    public static partial class TypeExtensions {
        public static object ConvertFromString(this Type type, string text)
            => type.GetConverter().ConvertFromString(text);

        public static TypeConverter GetConverter(this Type type) => TypeDescriptor.GetConverter(type);
    }
}