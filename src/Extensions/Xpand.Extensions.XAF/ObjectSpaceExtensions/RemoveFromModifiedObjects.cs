using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static void RemoveFromModifiedObjects(this IObjectSpaceLink objectSpaceLink)
            => objectSpaceLink.ObjectSpace.RemoveFromModifiedObjects(objectSpaceLink);
    }
}