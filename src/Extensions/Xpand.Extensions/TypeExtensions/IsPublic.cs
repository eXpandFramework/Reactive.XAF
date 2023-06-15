using System.Reflection;

namespace Xpand.Extensions.TypeExtensions {
    public static partial class TypeExtensions {
        public static bool IsPublic(this MemberInfo memberInfo) 
            => memberInfo switch {
                FieldInfo fieldInfo => fieldInfo.IsPublic,
                PropertyInfo propertyInfo => propertyInfo.GetGetMethod()?.IsPublic == true || propertyInfo.GetSetMethod()?.IsPublic == true,
                MethodInfo methodInfo => methodInfo.IsPublic,
                EventInfo eventInfo => eventInfo.GetAddMethod()?.IsPublic == true || eventInfo.GetRemoveMethod()?.IsPublic == true,
                _ => false
            };
    }
}