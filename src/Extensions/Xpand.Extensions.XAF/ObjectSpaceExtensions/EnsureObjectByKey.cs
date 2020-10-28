using System;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static T EnsureObjectByKey<T>(this IObjectSpace objectSpace, object key)
            => objectSpace.GetObjectByKey<T>(key) ?? objectSpace.NewObject<T>(key);
        
        public static object EnsureObjectByKey(this IObjectSpace objectSpace, Type objectType, object key)
            => objectSpace.GetObjectByKey(objectType, key);
        
    }
}