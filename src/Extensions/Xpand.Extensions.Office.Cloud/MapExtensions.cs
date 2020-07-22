using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base.General;
using Fasterflect;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.Office.Cloud.BusinessObjects;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Office.Cloud{
    public static class MapExtensions{
        public static IObservable<(IObjectSpace objectSpace, IEvent local, TCloudEvent cloud, MapAction mapAction)> SynchronizeLocalEvent<TCloudEvent, TService>(
            this IObservable<TService> source, Func<IObjectSpace> objectSpaceFactory, Guid currentUserId, Func<TService, ITokenStore, IObservable<
                (TCloudEvent @event,MapAction mapAction)>> modifiedEventsList, Type localEventType)  
            => Observable.Using(objectSpaceFactory, objectSpace => source.SelectMany(service => modifiedEventsList(service, objectSpace.CloudOfficeTokenStorage(currentUserId)))
                .SynchronizeLocalEvent(objectSpaceFactory, localEventType));

        public static CloudOfficeTokenStorage CloudOfficeTokenStorage(this IObjectSpace objectSpace,Guid currentUserId){
            var tokenStorage = (objectSpace.GetObjectByKey<CloudOfficeTokenStorage>(currentUserId) ?? objectSpace.CreateObject<CloudOfficeTokenStorage>());
            tokenStorage.Oid = currentUserId;
            objectSpace.CommitChanges();
            return tokenStorage;
        }

        static IObservable<(IObjectSpace objectSpace, IEvent source, TCloudEvent @event, MapAction mapAction)> AddLocalEvent<TCloudEvent>
            (this IObservable<(IObjectSpace objectSpace, IEvent source, TCloudEvent @event, MapAction mapAction)> source, TCloudEvent cloudEvent, IObjectSpace objectSpace)
            => source.SwitchIfEmpty((objectSpace,(IEvent)null,cloudEvent,MapAction.Insert).ReturnObservable().Select(tuple => tuple));

        static IObservable<(IObjectSpace objectSpace, IEvent source, TCloudEvent @event, MapAction mapAction)>
            SynchronizeLocalEvent<TCloudEvent>(this IObservable<(TCloudEvent @event, MapAction mapAction)> source,
                Func<IObjectSpace> objectSpaceFactory, Type localEventType) 
            => source.SelectMany(_ => Observable.Using(objectSpaceFactory, objectSpace => objectSpace
                .QueryCloudOfficeObject((string)_.@event.GetPropertyValue("Id"), _.@event.GetType().ToCloudObjectType()).ToObservable()
                .Select(cloudObject => ((IEvent) objectSpace.GetObjectByKey(localEventType,new Guid(cloudObject.LocalId)))).Pair(_)
                .Select(tuple => (objectSpace,tuple.source,tuple.other.@event,tuple.other.mapAction))
                .AddLocalEvent(_.@event,objectSpace)
                .Finally(() => objectSpace.CommitChanges())));

        public static IObservable<(TCloudEntity serviceObject, MapAction mapAction)>
            SynchronizeCloud<TCloudEntity, TLocalEntity>(this Func<IObjectSpace> objectSpaceFactory, SynchronizationType synchronizationType, IObjectSpace objectSpace,
            Func<string, IObservable<Unit>> deleteReqest, Func<TCloudEntity, IObservable<TCloudEntity>> insertReqest, Func<string, IObservable<TCloudEntity>> getRequest,
            Func<(TCloudEntity cloudEntity, TLocalEntity localEntity, string cloudId), IObservable<TCloudEntity>> updateRequest, 
            Action<GenericEventArgs<(CloudOfficeObject cloudOfficeObject, TLocalEntity localEntinty)>> onDelete = null, Action<(TCloudEntity target, TLocalEntity source)> onInsert = null,
            Action<(TCloudEntity target, TLocalEntity source)> update = null) where TCloudEntity : class
            => objectSpace.ModifiedObjects<TLocalEntity, TCloudEntity>(synchronizationType, cloudOfficeObject 
                    => cloudOfficeObject.Delete<TCloudEntity,TLocalEntity>(onDelete, deleteReqest).FirstOrDefaultAsync().Select(entity => (entity,MapAction.Delete)),
                localEntity => objectSpaceFactory.MapEntity(localEntity, sourceEntity 
                        => sourceEntity.Insert(objectSpace, objectSpaceFactory, onInsert, insertReqest).Select(entity => (entity, MapAction.Insert)), _ 
                        => _.task.Update(_.cloudId, getRequest, updateRequest, update).Select(entity => (entity, MapAction.Update))
                ));

        private static IObservable<TCloudEntity> Update<TCloudEntity, TLocalEntity>(this TLocalEntity source, string cloudId, Func<string, IObservable<TCloudEntity>> getRequest,
	        Func<(TCloudEntity cloudEntity, TLocalEntity localEntity, string cloudId), IObservable<TCloudEntity>> updateRequest, 
	        Action<(TCloudEntity target, TLocalEntity source)> update = null) 
            => getRequest(cloudId).SelectMany(target => {
                    update?.Invoke((target, source));
			        return updateRequest.Start(source, cloudId,  target);
		        });

        private static IObservable<TCloudEntity> Start<TCloudEntity, TLocalEntity>(this Func<(TCloudEntity cloudEntity, TLocalEntity localEntity, string cloudId), 
            IObservable<TCloudEntity>> updateRequest,TLocalEntity source, string cloudId,  TCloudEntity target) 
            => AppDomain.CurrentDomain.IsHosted() ? Observable.Start(() 
                => updateRequest((target, source, cloudId)).Wait()) : updateRequest((target, source, cloudId));

        private static IObservable<TCloudEntity> Insert<TCloudEntity, TLocalEntity>(this TLocalEntity sourceEvent, IObjectSpace objectSpace, Func<IObjectSpace> objectSpaceFactory,
            Action<(TCloudEntity, TLocalEntity)> insert, Func<TCloudEntity, IObservable<TCloudEntity>> insertReqest){
            var cloudEntity = (TCloudEntity)typeof(TCloudEntity).CreateInstance();
            insert?.Invoke((cloudEntity, sourceEvent));
            return insertReqest.Start(cloudEntity).NewCloudObject(objectSpaceFactory, objectSpace.GetKeyValue(sourceEvent).ToString());
        }

        private static IObservable<TCloudEntity> Start<TCloudEntity>(this Func<TCloudEntity, IObservable<TCloudEntity>> insertReqest, TCloudEntity updatedEvent) 
            => AppDomain.CurrentDomain.IsHosted()? Observable.Start(() 
                => insertReqest(updatedEvent).Wait()):insertReqest(updatedEvent);

        private static IObservable<TCloudEntity> Delete<TCloudEntity, TLocalEntity>(this (CloudOfficeObject cloudOfficeObject, TLocalEntity localEntinty) t,
            Action<GenericEventArgs<(CloudOfficeObject cloudOfficeObject, TLocalEntity localEntinty)>> delete,
            Func<string, IObservable<Unit>> deleteReqest) where TCloudEntity : class{
            var args = new GenericEventArgs<(CloudOfficeObject cloudOfficeObject,TLocalEntity localEntinty)>(t);
            delete?.Invoke(args);
            return !args.Handled ? deleteReqest.Start<TCloudEntity>(t.cloudOfficeObject) : Observable.Empty<TCloudEntity>();
        }

        private static IObservable<TCloudEntity> Start<TCloudEntity>(this Func<string, IObservable<Unit>> deleteReqest,CloudOfficeObject cloudOfficeObject) where TCloudEntity : class 
            => AppDomain.CurrentDomain.IsHosted()? Observable.Start(() 
                    => deleteReqest(cloudOfficeObject.CloudId).Select(entity => (TCloudEntity)null).Wait())
                :deleteReqest(cloudOfficeObject.CloudId).Select(entity => (TCloudEntity)null);
    }

    public enum MapAction{
        Delete,
        Insert,
        Update
    }
    public enum SynchronizationType{
        All,
        Created,
        Updated,
        Deleted,
        CreatedOrUpdated,
        CreatedOrDeleted,
        UpdatedOrDeleted
    }

}