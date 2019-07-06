using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{
    public static partial class TypeMappingService{
        private static void BrowsableRule((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) info){
            for (var index = info.propertyInfos.Count - 1; index >= 0; index--){
                var propertyInfo = info.propertyInfos[index];
                var isnotBrowsaable = propertyInfo.GetCustomAttributesData().Any(data =>
                    typeof(BrowsableAttribute).IsAssignableFrom(data.AttributeType) &&
                    data.ConstructorArguments.Any(argument => false.Equals(argument.Value)));
                if (isnotBrowsaable){
                    info.propertyInfos.Remove(propertyInfo);
                }
            }
        }

    }
}