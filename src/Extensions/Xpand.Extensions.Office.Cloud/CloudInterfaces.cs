using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base.General;
using Xpand.Extensions.TypeExtensions;

namespace Xpand.Extensions.Office.Cloud{
    public enum CloudObjectType{
        Event,
        Task
    }

    public static class CloudObjectExtensions{
        public static CloudObjectType ToCloudObjectType(this Type type){
            if (type.InheritsFrom("Microsoft.Graph.Event")||type.InheritsFrom("Google.Apis.Calendar.v3.Data.Event") || typeof(IEvent).IsAssignableFrom(type)){
                return CloudObjectType.Event;
            }
            if (type.InheritsFrom("Microsoft.Graph.OutlookTask")||type.InheritsFrom("Google.Apis.Tasks.v1.Data") || typeof(ITask).IsAssignableFrom(type)){
                return CloudObjectType.Task;
            }
            throw new NotSupportedException(type.FullName);
        }
        
        public static IQueryable<CloudOfficeObject> QueryCloudOfficeObject(this IObjectSpace objectSpace, string cloudId, CloudObjectType cloudObjectType) 
            => objectSpace.GetObjectsQuery<CloudOfficeObject>().Where(o => o.CloudObjectType == cloudObjectType && o.CloudId == cloudId);

        public static IQueryable<CloudOfficeObject> QueryCloudOfficeObject(this IObjectSpace objectSpace, string localId, Type cloudEntityType){
            var cloudObjectType = cloudEntityType.ToCloudObjectType();
            return objectSpace.GetObjectsQuery<CloudOfficeObject>().Where(o => o.CloudObjectType == cloudObjectType && o.LocalId == localId);
        }

        public static IQueryable<CloudOfficeObject> QueryCloudOfficeObject(this IObjectSpace objectSpace, Type cloudEntityType, object localEntity){
            var localId = objectSpace.GetKeyValue(localEntity);
            return objectSpace.QueryCloudOfficeObject(localId.ToString(), cloudEntityType);
        }

    }
}