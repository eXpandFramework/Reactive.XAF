using System;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Validation;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static object NewObject(this IObjectSpace objectSpace,Type type, object key) {
            var o = objectSpace.CreateObject(type);
            objectSpace.TypesInfo.FindTypeInfo(type).KeyMember.SetValue(o, key);
            return o;
        }

        public static T NewObject<T>(this IObjectSpace objectSpace, object key) {
            var o = objectSpace.CreateObject<T>();
            objectSpace.TypesInfo.FindTypeInfo(typeof(T)).KeyMember.SetValue(o, key);
            return o;
        }

        
    }
}