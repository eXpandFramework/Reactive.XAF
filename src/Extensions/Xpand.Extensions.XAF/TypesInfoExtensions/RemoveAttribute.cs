using System;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.TypesInfoExtensions {
    public static partial class TypesInfoExtensions {
        public static void RemoveAttribute(this IBaseInfo info, Attribute attribute) 
            => ((BaseInfo) info).RemoveAttribute(attribute);
    }
}