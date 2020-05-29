using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base.General;
using Xpand.Extensions.TypeExtensions;

namespace Xpand.Extensions.Office.Cloud{
    public enum CloudObjectType{
        Event,
        Task
    }
    public enum CloudProvider{
        Google,
        Microsoft
    }

    public static class CloudObjectExtensions{
        public static CloudObjectType ToCloudObjectType(this System.Type type){
            if (type.InheritsFrom("Microsoft.Graph.Event")||type.InheritsFrom("Google.Apis.Calendar.v3.Data.Event") || typeof(IEvent).IsAssignableFrom(type)){
                return CloudObjectType.Event;
            }
            if (type.InheritsFrom("Microsoft.Graph.OutlookTask")||type.InheritsFrom("Google.Apis.Tasks.v1.Data") || typeof(ITask).IsAssignableFrom(type)){
                return CloudObjectType.Task;
            }
            throw new NotSupportedException(type.FullName);
        }

        public static IQueryable<CloudOfficeObject> QueryCloudOfficeObject(this IObjectSpace objectSpace,
            string cloudId, CloudObjectType cloudObjectType) => objectSpace.GetObjectsQuery<CloudOfficeObject>().Where(o => o.CloudObjectType == cloudObjectType && o.CloudId == cloudId);

        public static IQueryable<CloudOfficeObject> QueryCloudOfficeObject(this IObjectSpace objectSpace, string localId, System.Type cloudEntityType){
            var cloudObjectType = cloudEntityType.ToCloudObjectType();
            return objectSpace.GetObjectsQuery<CloudOfficeObject>().Where(o => o.CloudObjectType == cloudObjectType && o.LocalId == localId);
        }

        public static IQueryable<CloudOfficeObject> QueryCloudOfficeObject(this IObjectSpace objectSpace, System.Type cloudEntityType, object localEntity){
            var localId = objectSpace.GetKeyValue(localEntity);
            return objectSpace.QueryCloudOfficeObject(localId.ToString(), cloudEntityType);
        }

    }
    public interface IEventAttendees{
        IEnumerable<IEventAttendee> Attendees { get; }
    }

    public interface IEventAttendee{
        string UserName { get; }
        string UserEmail { get; }
        EventResourceResponse EventResourceStatus { get; set; }
    }

    public interface ICloudUser : IObjectSpaceLink{
        string UserName { get; }
        CloudProvider? CloudProvider { get; }
    }

    public enum EventResourceResponse{
        NeedsAction = 0,
        Tentative = 1,
        Accepted = 2,
        Declined = 3,

    }

}