using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.TypesInfoExtensions {
    public static partial class TypesInfoExtensions {
        public static IEnumerable<(string Name, object value)> NameValues(this IEnumerable<IMemberInfo> source, object instance)
            => source.Values(instance).Select(t => (t.info.Name,t.value));
        
        public static IEnumerable<(IMemberInfo info, object value)> Values(this IEnumerable<IMemberInfo> source, object instance)
            => source.Select(info => (info,value:info.GetValue(instance)));
        
        public static IEnumerable<string> ToDefaultValues(this IEnumerable<object> source, ITypeInfo typeInfo)
            => source.Select(s =>typeInfo.IsDomainComponent? $"{typeInfo.DefaultMember.GetValue(s)}":$"{s}");
    }
}