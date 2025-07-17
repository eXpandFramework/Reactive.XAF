using System;
using System.Collections;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static IList GetObjects(this IObjectSpace objectSpace, Type type, CriteriaOperator criteria, int topReturnObjects,int skipReturnedObjects) 
            => objectSpace.GetObjects(type, criteria, topReturnObjects, skipReturnedObjects,
                new SortProperty(objectSpace.TypesInfo.FindTypeInfo(type).KeyMember.Name, SortingDirection.Ascending));

        public static IList GetObjects(this IObjectSpace objectSpace, Type type, CriteriaOperator criteria, int topReturnObjects,int skipReturnedObjects,params SortProperty[] properties) {
            var objects = objectSpace.GetObjects(type,criteria,topReturnObjects);
            var baseCollection = (XPBaseCollection)objects;
            baseCollection!.SkipReturnedObjects = skipReturnedObjects;
            baseCollection.Sorting=new SortingCollection(properties);
            return objects;
        }
    }
}