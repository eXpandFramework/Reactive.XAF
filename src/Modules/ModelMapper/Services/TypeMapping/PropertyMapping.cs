using System.ComponentModel;
using System.Reflection;
using Fasterflect;

namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{

    public static partial class TypeMappingService{
        private static bool BrowsableRule(PropertyInfo propertyInfo){
            var browsableAttribute = propertyInfo.Attribute<BrowsableAttribute>();
            return browsableAttribute == null || browsableAttribute.Browsable;
        }
    }
}