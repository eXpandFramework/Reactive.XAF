using System;
using System.Linq;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.TypesInfoExtensions {
    public static partial class TypesInfoExtensions {
        public static Type[] PersistentTypes(this ITypesInfo typesInfo, Type type)
            => typesInfo.PersistentTypes
                .Where(info => type.IsAssignableFrom(info.Type) && info.IsPersistent && !info.IsAbstract)
                .Select(info => info.Type).ToArray();
    }
}