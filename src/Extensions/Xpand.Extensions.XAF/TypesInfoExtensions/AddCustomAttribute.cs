using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.XAF.Attributes.Custom;

namespace Xpand.Extensions.XAF.TypesInfoExtensions {
    public static partial class TypesInfoExtensions {
        public static ICustomAttribute AddCustomAttribute(this IBaseInfo baseInfo, ICustomAttribute attribute) {
            for (int index = 0; index < attribute.Name.Split(';').Length; index++) {
                string s = attribute.Name.Split(';')[index];
                var theValue = attribute.Value.Split(';')[index];
                baseInfo.AddAttribute(new ModelDefaultAttribute(s, theValue));
            }

            return attribute;
        }
    }
}