using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static T GetObject<T>(this T link, IObjectSpace objectSpace)
            => objectSpace.GetObject(link);
        public static T GetObject<T>(this T link, IObjectSpaceLink objectSpaceLink)
            => link.GetObject(objectSpaceLink.ObjectSpace);
        public static T GetObjectFromKey<T>(this IObjectSpace objectSpace, T instance)
            => (T)objectSpace.GetObjectByKey(instance.GetType(), objectSpace.GetKeyValue(instance));
    }
}