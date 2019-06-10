using System;

namespace Xpand.Source.Extensions.System.Refelction{
    internal static partial class ReflectionExtensions{
        public static bool IsNullableType(this Type theType){
            return theType.IsGenericType && theType.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }
}