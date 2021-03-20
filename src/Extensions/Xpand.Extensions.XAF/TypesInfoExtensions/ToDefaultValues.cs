using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.TypesInfoExtensions {
    public static partial class TypesInfoExtensions {
        public static IEnumerable<string> ToDefaultValues(this IEnumerable<object> source, ITypeInfo typeInfo)
            => source.Select(s =>typeInfo.IsDomainComponent? $"{typeInfo.DefaultMember.GetValue(s)}":$"{s}");
    }
}