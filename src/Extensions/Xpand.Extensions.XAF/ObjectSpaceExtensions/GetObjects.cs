using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.TypesInfoExtensions;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static T[] GetObjects<T>(this IObjectSpace objectSpace, params T[] objects) 
            => objects.GroupBy(arg => arg.GetType().ToTypeInfo())
                .SelectMany(grouping => {
                    var keyMember = grouping.Key.Type.ToTypeInfo().KeyMember;
                    return grouping.Buffer(1000)
                        .SelectMany(buffer => objectSpace.GetObjects(grouping.Key.Type,
                            new InOperator(keyMember.Name, buffer.Select(arg => keyMember.GetValue(arg)))).Cast<T>());
                })
                .ToArray();
    }
}