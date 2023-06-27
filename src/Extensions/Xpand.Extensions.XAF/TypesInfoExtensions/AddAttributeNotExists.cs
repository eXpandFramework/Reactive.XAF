using System;
using System.Linq;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.TypesInfoExtensions {
    public static partial class TypesInfoExtensions {
        public static void AddAttributeNotExists(this IBaseInfo baseInfo, Attribute attribute) {
            if (baseInfo.FindAttributes<Attribute>().All(attribute1 => attribute1.Match(attribute))) {
                baseInfo.AddAttribute(attribute);
            }
        }
    }
}