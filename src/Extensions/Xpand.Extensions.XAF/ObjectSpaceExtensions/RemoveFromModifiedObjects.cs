using System.Collections;
using System.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static void RemoveFromModifiedObjects(this IObjectSpaceLink objectSpaceLink)
            => objectSpaceLink.ObjectSpace.RemoveFromModifiedObjects(objectSpaceLink);
        public static T Validate<T>(this T objectSpaceLink)  where T : IObjectSpaceLink{
            var typeInfo = objectSpaceLink.ObjectSpace.TypesInfo.FindTypeInfo(objectSpaceLink.GetType());
            var collectionObjects = typeInfo.Members.Where(info => info.IsAggregated&&info.IsAssociation&&info.IsList)
                .SelectMany(info => ((IEnumerable)info.GetValue(objectSpaceLink)).Cast<object>())
                .AddToArray(objectSpaceLink);
            objectSpaceLink.ObjectSpace.Validate(collectionObjects);
            return objectSpaceLink;
        }
    }
}