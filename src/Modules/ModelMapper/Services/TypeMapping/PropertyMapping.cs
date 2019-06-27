using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Fasterflect;

namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{

    public static partial class TypeMappingService{
        private static void BrowsableRule(List<PropertyInfo> propertyInfos){
            for (var index = propertyInfos.Count - 1; index >= 0; index--){
                var propertyInfo = propertyInfos[index];
                var browsableAttribute = propertyInfo.Attribute<BrowsableAttribute>();
                if (browsableAttribute != null && !browsableAttribute.Browsable){
                    propertyInfos.Remove(propertyInfo);
                }
            }
        }
    }
}