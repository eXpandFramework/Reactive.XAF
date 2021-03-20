using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Xpand.Extensions.TypeExtensions{
    [PublicAPI]
    public static partial class TypeExtensions{
        public static IEnumerable<Type> ParentTypes(this Type type){
            if (type == null){
                yield break;
            }
            foreach (var i in type.GetInterfaces()){
                yield return i;
            }
            var currentBaseType = type.BaseType;
            while (currentBaseType != null){
                yield return currentBaseType;
                currentBaseType= currentBaseType.BaseType;
            }
        }
    }
    
}