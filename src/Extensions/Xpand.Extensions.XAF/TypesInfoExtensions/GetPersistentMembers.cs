using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.TypesInfoExtensions {
    public static partial class TypesInfoExtensions {
        public static IEnumerable<(TAttribute attribute, IMemberInfo info)> PersistentTypeMembers<TAttribute>(this ITypesInfo typesInfo) where TAttribute : Attribute
            => typesInfo.PersistentTypes.SelectMany(info => info.Members).SelectMany(info => info.FindAttributes<TAttribute>().Select(attribute => (attribute, info)));
        
        public static IEnumerable<(T attribute, IMemberInfo info)> Members<T>(this ITypeInfo typeInfo) where T : Attribute
            => typeInfo.Members.SelectMany(info => info.FindAttributes<T>().Select(attribute => (attribute, info)));
    }
}