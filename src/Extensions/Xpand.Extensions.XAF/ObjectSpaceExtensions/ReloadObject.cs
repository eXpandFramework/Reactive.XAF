using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static T ReloadObject<T>(this T objectSpaceLink) where T:IObjectSpaceLink
            => (T)objectSpaceLink.ObjectSpace.ReloadObject(objectSpaceLink);
    }
}