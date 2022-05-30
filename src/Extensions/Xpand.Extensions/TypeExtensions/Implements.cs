using System;
using System.Linq;

namespace Xpand.Extensions.TypeExtensions {
    public static partial class TypeExtensions {
        public static bool Implements(this Type type, params string[] typeNames)
            => type.GetInterfaces().Any(type1 => typeNames.Contains(type1.FullName));
    }
}