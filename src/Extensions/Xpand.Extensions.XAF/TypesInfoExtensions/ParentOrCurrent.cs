using System.Linq;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.TypesInfoExtensions {
    public static partial class TypesInfoExtensions {
        public static IMemberInfo ParentOrCurrent(this IMemberInfo memberInfo)
            => (memberInfo.GetPath().SkipLast(1).FirstOrDefault() ?? memberInfo);
    }
}