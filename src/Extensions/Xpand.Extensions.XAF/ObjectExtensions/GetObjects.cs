using System;
using System.Collections;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static IList GetObjects(this IObjectSpace objectSpace,Type type, CriteriaOperator criteria, int topReturnObjects) {
            var objects = objectSpace.GetObjects(type, criteria);
            objectSpace.SetTopReturnedObjectsCount(objects,topReturnObjects);
            return objects;
        }
    }
}