using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.TypesInfoExtensions {
    public static partial class TypesInfoExtensions {
        public static IEnumerable<(TAttribute attribute, IMemberInfo info)> Members<TAttribute>(this ITypesInfo typesInfo) where TAttribute : Attribute
            => typesInfo.PersistentTypes.SelectMany(info => info.Members).SelectMany(info => info.FindAttributes<TAttribute>().Select(attribute => (attribute, info)));
    }
}