using System;
using System.Collections.Generic;

namespace Xpand.Extensions.TypeExtensions {
    public static partial class TypeExtensions {
        private static HashSet<Type> _valueTupleSet = new HashSet<Type>(
            new[] {
                typeof(ValueTuple<>), typeof(ValueTuple<,>),
                typeof(ValueTuple<,,>), typeof(ValueTuple<,,,>),
                typeof(ValueTuple<,,,,>), typeof(ValueTuple<,,,,,>),
                typeof(ValueTuple<,,,,,,>), typeof(ValueTuple<,,,,,,,>),
                typeof(Tuple<>), typeof(Tuple<,>),
                typeof(Tuple<,,>), typeof(Tuple<,,,>),
                typeof(Tuple<,,,,>), typeof(Tuple<,,,,,>),
                typeof(Tuple<,,,,,,>), typeof(Tuple<,,,,,,,>)

            }
        );
        public static bool IsTupleFamily(this Type type) 
            => type.IsGenericType && _valueTupleSet.Contains(type.GetGenericTypeDefinition());
    }
}