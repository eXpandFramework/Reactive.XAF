using System;
using System.Linq;

namespace Xpand.Extensions.TypeExtensions{
    public static partial class TypeExtensions{
        public static bool InheritsFrom(this Type type, string typeName) => type
            .FullName==typeName|| type.ParentTypes().Select(_ => _.FullName).Any(s => typeName.Equals(s,StringComparison.Ordinal));

        public static bool InheritsFrom(this Type type, Type baseType){
            if (type == null){
                return false;
            }

            if (type == baseType){
                return true;
            }
            if (baseType == null){
                return type.IsInterface || type == typeof(object);
            }
            if (baseType.IsInterface){
                return type.GetInterfaces().Contains(baseType);
            }
            var currentType = type;
            while (currentType != null){
                if (currentType.BaseType == baseType){
                    return true;
                }
                currentType = currentType.BaseType;
            }
            return false;
        }
    }
}