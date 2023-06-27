using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.TypesInfoExtensions {
    public static partial class TypesInfoExtensions {
        public static IEnumerable<ITypeInfo> NonAbstractDescendants(this ITypeInfo typeInfo) 
            => typeInfo.Descendants.Where(info => !info.IsAbstract);
    }
}