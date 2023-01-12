using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static T GetObjectFromKey<T>(this IObjectSpace objectSpace, T instance)
            => (T)objectSpace.GetObjectByKey(instance.GetType(), objectSpace.GetKeyValue(instance));
    }
}