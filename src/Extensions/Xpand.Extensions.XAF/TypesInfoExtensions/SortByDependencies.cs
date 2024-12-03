using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.TypesInfoExtensions {
    public static partial class TypesInfoExtensions {
        public static IList<ITypeInfo> SortByDependencies(this IList<ITypeInfo> typeInfos) {
            var dependencyMap = typeInfos.ToDictionary(type => type,
                type => type.Members.Where(info => info.IsPersistent).Select(info => info.MemberTypeInfo)
                    .Where(typeInfos.Contains).ToList()
            );

            var infos = new List<ITypeInfo>();
            var infosCount = typeInfos.Count;
            while (infos.Count < infosCount) {
                var typeInfo = typeInfos.FirstOrDefault(info => !dependencyMap[info].Contains(info));
                var index = infos.FindIndex(info => dependencyMap[info].Contains(typeInfo));
                if (index >= 0) {
                    infos.Insert(index, typeInfo);
                }
                else {
                    infos.Add(typeInfo);
                }

                typeInfos.Remove(typeInfo);
            }

            return infos;
        }
    }
}