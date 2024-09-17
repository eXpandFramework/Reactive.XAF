using System;
using System.Linq;
using DevExpress.ExpressApp.DC;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.TypesInfoExtensions {
    public static partial class TypesInfoExtensions {
        public static Type[] DomainComponents(this ITypesInfo typesInfo, Type type)
            => type.IsInterface ? typesInfo.PersistentTypes.Where(info => type.IsAssignableFrom(info.Type) && info.IsPersistent && !info.IsAbstract)
                    .Select(info => info.Type).Distinct().ToArray() : type.YieldItem().ToArray();
    }
}