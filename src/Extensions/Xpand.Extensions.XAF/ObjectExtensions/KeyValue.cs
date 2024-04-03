using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static object DCKeyValue(this object source)
            => source.GetTypeInfo().KeyMember.GetValue(source);
        
        public static object KeyValue(this IObjectSpaceLink source)
            => source.ObjectSpace.GetKeyValue(source);
        public static T KeyValue<T>(this IObjectSpaceLink source)
            => (T)source.ObjectSpace.GetKeyValue(source);
    }
}