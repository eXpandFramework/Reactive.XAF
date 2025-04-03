using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;

namespace Xpand.Extensions.XAF.TypesInfoExtensions {
    public static partial class TypesInfoExtensions {
        public static IMemberInfo FindDisplayableMember(this IMemberInfo memberInfo) 
            => ReflectionHelper.FindDisplayableMemberDescriptor(memberInfo);
    }
}