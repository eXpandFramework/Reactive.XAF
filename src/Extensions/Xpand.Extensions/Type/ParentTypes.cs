using System.Collections.Generic;
using JetBrains.Annotations;

namespace Xpand.Extensions.Type{
    [PublicAPI]
    public static partial class TypeExtensions{
        public static IEnumerable<System.Type> ParentTypes(this System.Type type){
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