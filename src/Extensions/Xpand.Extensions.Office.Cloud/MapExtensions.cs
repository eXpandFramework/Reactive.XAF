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
            (this IObservable<(IObjectSpace objectSpace, IEvent source, TCloudEvent @event, MapAction mapAction)> source,
                TCloudEvent cloudEvent, IObjectSpace objectSpace)
            => source.SwitchIfEmpty((objectSpace,(IEvent)null,cloudEvent,MapAction.Insert).ReturnObservable().Select(tuple => tuple));

        static IObservable<(IObjectSpace objectSpace, IEvent source, TCloudEvent @event, MapAction mapAction)>
            SynchronizeLocalEvent<TCloudEvent>(this IObservable<(TCloudEvent @event, MapAction mapAction)> source,
                Func<IObjectSpace> objectSpaceFactory, Type localEventType) 
            => source.SelectMany(_ => Observable.Using(objectSpaceFactory, objectSpace => objectSpace
                .QueryCloudOfficeObject((string)_.@event.GetPropertyValue("Id"), _.@event.GetType().ToCloudObjectType()).ToObservable()
                .Select(cloudObject => ((IEvent) objectSpace.GetObjectByKey(localEventType,new Guid(cloudObject.LocalId)))).Pair(_)
                .Select(tuple => (objectSpace,tuple.source,tuple.other.@event,tuple.other.mapAction))
                .AddLocalEvent(_.@event,objectSpace)
                .Finally(objectSpace.CommitChanges)));

        public static IObservable<(TCloudEntity serviceObject, MapAction mapAction)> Synchronize<TCloudEntity, TSourceEntity>(this Func<IObjectSpace> objectSpaceFactory, IObjectSpace objectSpace,
            Func<string, IObservable<Unit>> deleteReqest, Func<TCloudEntity, IObservable<TCloudEntity>> insertReqest, Func<string, IObservable<TCloudEntity>> getRequest,
            Func<(TCloudEntity cloudEntity, TSourceEntity localEntity, string cloudId), IObservable<TCloudEntity>> updateRequest, Func<MapAction,TCloudEntity, TSourceEntity, TCloudEntity> map,
            Action<GenericEventArgs<CloudOfficeObject>> onDelete = null, Action<(TCloudEntity target, TSourceEntity source)> onInsert = null,
            Action<(TCloudEntity target, TSourceEntity source)> update = null) where TCloudEntity : class
            => objectSpace.MapEntities<TSourceEntity, TCloudEntity>(
                cloudOfficeObject => cloudOfficeObject.Delete<TCloudEntity>(onDelete, deleteReqest).FirstOrDefaultAsync().Select(entity => (entity,MapAction.Delete)),
                localEntity => objectSpaceFactory.MapEntity(localEntity, sourceEntity 
                        => sourceEntity.Insert(objectSpace, objectSpaceFactory, onInsert, insertReqest, (cloud, local) => map(MapAction.Insert, cloud,local)).Select(entity => (entity, MapAction.Insert)), _ 
                        => _.task.Update(_.cloudId, getRequest, updateRequest, (cloud, local) =>map(MapAction.Update, cloud,local) , update).Select(entity => (entity, MapAction.Update))
                ));

        private static IObservable<TCloudEntity> Update<TCloudEntity, TLocalEntity>(this TLocalEntity source, string cloudId, Func<string, IObservable<TCloudEntity>> getRequest,
	        Func<(TCloudEntity cloudEntity, TLocalEntity localEntity, string cloudId), IObservable<TCloudEntity>> updateRequest, Func<TCloudEntity, TLocalEntity, TCloudEntity> map,
	        Action<(TCloudEntity target, TLocalEntity source)> update = null) 
            => getRequest(cloudId).SelectMany(target => {
			        target = map(target, source);
			        update?.Invoke((target, source));
			        return updateRequest.Start(source, cloudId,  target);
		        });

        private static IObservable<TCloudEntity> Start<TCloudEntity, TLocalEntity>(this Func<(TCloudEntity cloudEntity, TLocalEntity localEntity, string cloudId), 
            IObservable<TCloudEntity>> updateRequest,TLocalEntity source, string cloudId,  TCloudEntity target) 
            => AppDomain.CurrentDomain.IsHosted() ? Observable.Start(() 
                => updateRequest((target, source, cloudId)).Wait()) : updateRequest((target, source, cloudId));

        private static IObservable<TCloudEntity> Insert<TCloudEntity, TLocalEntity>(this TLocalEntity sourceEvent, IObjectSpace objectSpace, Func<IObjectSpace> objectSpaceFactory,
            Action<(TCloudEntity, TLocalEntity)> insert, Func<TCloudEntity, IObservable<TCloudEntity>> insertReqest, Func<TCloudEntity, TLocalEntity, TCloudEntity> map){
            var cloudEntity = (TCloudEntity)typeof(TCloudEntity).CreateInstance();
            var updatedEvent = map(cloudEntity, sourceEvent);
            insert?.Invoke((updatedEvent, sourceEvent));
            return insertReqest.Start( updatedEvent).NewCloudObject(objectSpaceFactory, objectSpace.GetKeyValue(sourceEvent).ToString());
        }

        private static IObservable<TCloudEntity> Start<TCloudEntity>(this Func<TCloudEntity, IObservable<TCloudEntity>> insertReqest, TCloudEntity updatedEvent) 
            => AppDomain.CurrentDomain.IsHosted()? Observable.Start(() 
                => insertReqest(updatedEvent).Wait()):insertReqest(updatedEvent);

        private static IObservable<TCloudEntity> Delete<TCloudEntity>(this CloudOfficeObject cloudOfficeObject, Action<GenericEventArgs<CloudOfficeObject>> delete,
            Func<string, IObservable<Unit>> deleteReqest) where TCloudEntity : class{
            var args = new GenericEventArgs<CloudOfficeObject>(cloudOfficeObject);
            delete?.Invoke(args);
            return !args.Handled ? deleteReqest.Start<TCloudEntity>(cloudOfficeObject) : Observable.Empty<TCloudEntity>();
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
}