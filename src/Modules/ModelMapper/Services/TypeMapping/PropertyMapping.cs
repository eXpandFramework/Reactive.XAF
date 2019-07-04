using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Fasterflect;

namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{
    public static partial class TypeMappingService{

        private static void BrowsableRule((Type declaringType, List<PropertyInfo> propertyInfos) info){
            for (var index = info.propertyInfos.Count - 1; index >= 0; index--){
                var propertyInfo = info.propertyInfos[index];
                var browsableAttribute = propertyInfo.Attribute<BrowsableAttribute>();
                if (browsableAttribute != null && !browsableAttribute.Browsable){
                    info.propertyInfos.Remove(propertyInfo);
                }
            }
        }
    }
}