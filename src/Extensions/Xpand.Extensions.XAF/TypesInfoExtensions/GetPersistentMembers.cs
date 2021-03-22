using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.TypesInfoExtensions {
    public static partial class TypesInfoExtensions {
        public static IEnumerable<(T attribute, ITypeInfo info)> GetPersistentMembers<T>(this ITypesInfo typesInfo) where T : Attribute
            => typesInfo.PersistentTypes.SelectMany(info => info.FindAttributes<T>().Select(attribute => (attribute, info)));
    }
}