using System;
using System.Collections;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base.General;
using Fasterflect;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.Office.Cloud.BusinessObjects;

namespace Xpand.Extensions.Office.Cloud{
    public static class MapExtensions{
        public static IObservable<TCloudEvent> SynchronizeEventResources<TLocalEvent, TCloudEvent, TCloudAttendee, TService>(this IObservable<TService> source,
            Func<IObjectSpace> objectSpaceFactory, Guid currentUserId, Func<TCloudAttendee, string> emailFactory,
            Action<IEventAttendee, TCloudAttendee> map, Func<TService, ITokenStore, IObservable<TCloudEvent>> modifiedEventsList) where TLocalEvent : IEvent, IEventAttendees => Observable.Using(objectSpaceFactory, objectSpace => {
                var tokenStorage = (objectSpace.GetObjectByKey<CloudOfficeTokenStorage>(currentUserId) ?? objectSpace.CreateObject<CloudOfficeTokenStorage>());
                tokenStorage.Oid = currentUserId;
                objectSpace.CommitChanges();
                return source.SelectMany(service => modifiedEventsList(service, tokenStorage))
                    .SynchronizeEventResources<TLocalEvent, TCloudEvent, TCloudAttendee>(objectSpaceFactory, emailFactory, map);
            });

        static IObservable<TCloudEvent> SynchronizeEventResources<TLocalEvent, TCloudEvent, TCloudAttendee>(this IObservable<TCloudEvent> source, Func<IObjectSpace> objectSpaceFactory,
            Func<TCloudAttendee, string> emailFactory, Action<IEventAttendee, TCloudAttendee> map) where TLocalEvent : IEvent, IEventAttendees{
            var scheduler = ImmediateScheduler.Instance;
            return source.Where(cloudEvent => cloudEvent.GetPropertyValue("Attendees") != null)
                .SelectMany(cloudEvent => Observable.Using(objectSpaceFactory, objectSpace => {
                    return objectSpace.QueryCloudOfficeObject((string)cloudEvent.GetPropertyValue("Id"), cloudEvent.GetType().ToCloudObjectType()).ToObservable(scheduler)
                        .SelectMany(cloudObject => ((IEnumerable)cloudEvent.GetPropertyValue("Attendees")).Cast<TCloudAttendee>().ToObservable(scheduler)
                            .SelectMany(cloudAttendee => objectSpace.GetObjectByKey<TLocalEvent>(new Guid(cloudObject.LocalId)).Attendees
                                .Where(eventAttendee => eventAttendee.UserEmail == emailFactory(cloudAttendee)).ToObservable(scheduler)
                                .Do(eventAttendee => map(eventAttendee, cloudAttendee))
                                .Finally(objectSpace.CommitChanges)
                                .Select(response => cloudEvent)));
                }));
        }

        public static IObservable<TCloudEntity> Synchronize<TCloudEntity, TSourceEntity>(this Func<IObjectSpace> objectSpaceFactory, IObjectSpace objectSpace,
            Func<string, IObservable<Unit>> deleteReqest, Func<TCloudEntity, IObservable<TCloudEntity>> insertReqest, Func<string, IObservable<TCloudEntity>> getRequest,
            Func<(TCloudEntity cloudEntity, TSourceEntity localEntity, string cloudId), IObservable<TCloudEntity>> updateRequest, Func<TCloudEntity, TSourceEntity, TCloudEntity> map,
            Action<GenericEventArgs<CloudOfficeObject>> onDelete = null, Action<(TCloudEntity target, TSourceEntity source)> onInsert = null,
            Action<(TCloudEntity target, TSourceEntity source)> update = null) where TCloudEntity : class 
            
            => objectSpace.MapEntities<TSourceEntity, TCloudEntity>(
                cloudOfficeObject => cloudOfficeObject.Delete<TCloudEntity>(onDelete, deleteReqest),
                localEntity => objectSpaceFactory.MapEntity(localEntity,
                    sourceEntity => sourceEntity.Insert(objectSpace, objectSpaceFactory, onInsert, insertReqest, map),
                    _ => _.task.Update(_.cloudId, getRequest, updateRequest, map, update)
                ));

        private static IObservable<TCloudEntity> Update<TCloudEntity, TLocalEntity>(this TLocalEntity source, string cloudId, Func<string, IObservable<TCloudEntity>> getRequest,
	        Func<(TCloudEntity cloudEntity, TLocalEntity localEntity, string cloudId), IObservable<TCloudEntity>> updateRequest, Func<TCloudEntity, TLocalEntity, TCloudEntity> map,
	        Action<(TCloudEntity target, TLocalEntity source)> update = null) => getRequest(cloudId)
		        .SelectMany(target => {
			        target = map(target, source);
			        update?.Invoke((target, source));
			        return updateRequest((target, source, cloudId));
		        });

        private static IObservable<TCloudEntity> Insert<TCloudEntity, TLocalEntity>(this TLocalEntity sourceEvent, IObjectSpace objectSpace, Func<IObjectSpace> objectSpaceFactory,
            Action<(TCloudEntity, TLocalEntity)> insert, Func<TCloudEntity, IObservable<TCloudEntity>> insertReqest, Func<TCloudEntity, TLocalEntity, TCloudEntity> map){
            var cloudEntity = (TCloudEntity)typeof(TCloudEntity).CreateInstance();
            var updatedEvent = map(cloudEntity, sourceEvent);
            insert?.Invoke((updatedEvent, sourceEvent));
            return insertReqest(updatedEvent).NewCloudObject(objectSpaceFactory, objectSpace.GetKeyValue(sourceEvent).ToString());
        }

        private static IObservable<TCloudEntity> Delete<TCloudEntity>(this CloudOfficeObject cloudOfficeObject, Action<GenericEventArgs<CloudOfficeObject>> delete,
            Func<string, IObservable<Unit>> deleteReqest) where TCloudEntity : class{
            var args = new GenericEventArgs<CloudOfficeObject>(cloudOfficeObject);
            delete?.Invoke(args);
            return !args.Handled ? deleteReqest(cloudOfficeObject.CloudId).Select(entity => (TCloudEntity)null) : Observable.Empty<TCloudEntity>();
        }

    }


}