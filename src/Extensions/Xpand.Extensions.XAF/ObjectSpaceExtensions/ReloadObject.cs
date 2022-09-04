using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static object ReloadObject(this IObjectSpaceLink objectSpaceLink)
            => objectSpaceLink.ObjectSpace.ReloadObject(objectSpaceLink);
    }
}