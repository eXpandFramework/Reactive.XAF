using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.TypeExtensions {
    public static partial class TypeExtensions {
        public static IEnumerable<string> InNamespace(this IEnumerable<Type> types)
            => types.SelectMany(type => type.Assembly.GetTypes().Where(t => t.Namespace == type.Namespace))
                .Select(t => t.Name);
    }
}