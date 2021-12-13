using Swordfish.NET.Collections.Auxiliary;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.Extensions.XAF.TypesInfoExtensions {
    public static partial class TypesInfoExtensions {
        public static void MapTypeInfoOwnMembers(this object source, object target)
            => source.GetTypeInfo().OwnMembers
                .ForEach(info => {
                    var targetMember = target.GetTypeInfo().FindMember(info.Name);
                    if (info.MemberType == targetMember.MemberType) {
                        targetMember.SetValue(target, info.GetValue(source));    
                    }
                });
    }
}