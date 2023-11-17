using System;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static bool Any<T>(this IObjectSpace objectSpace,Expression<Func<T,bool>> criteria=null) 
            =>criteria==null? objectSpace.GetObjectsQuery<T>().Any():objectSpace.GetObjectsQuery<T>().Any(criteria);

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