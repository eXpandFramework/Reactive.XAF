using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static T GetObject<T>(this T link, IObjectSpace objectSpace)
            => objectSpace.GetObject(link);
        
        public static T GetObject<T>(this IObjectSpaceLink link, T value)
            => value.GetObject(link.ObjectSpace);

        public static T GetObjectFromKey<T>(this IObjectSpace objectSpace, T instance)
            => instance == null ? default: (T)objectSpace.GetObjectByKey(instance.GetType(), objectSpace.GetKeyValue(instance));
    }
}