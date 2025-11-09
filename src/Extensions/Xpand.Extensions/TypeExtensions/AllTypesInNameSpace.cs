using System;
using System.Collections.Generic;
using System.Linq;
using Xpand.Extensions.AssemblyExtensions;

namespace Xpand.Extensions.TypeExtensions {
    public static partial class TypeExtensions {
        public static IEnumerable<Type> AllTypesInNameSpace(this Type type)
            => type.Assembly.GetTypesFromAssembly().Where(type1
                => type1.Namespace?.StartsWith(type.Namespace!, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}