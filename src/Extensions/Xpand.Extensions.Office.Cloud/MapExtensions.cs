﻿using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
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
            this IObservable<TService> source, Func<IObjectSpace> objectSpaceFactory, Guid currentUserId, Func<TService, ICloudOfficeToken, IObservable<
                (TCloudEvent @event,MapAction mapAction)>> modifiedEventsList, Type localEventType,string tokenType)
            => Observable.Using(objectSpaceFactory, objectSpace => source.SelectMany(service => modifiedEventsList(service,
                    objectSpace.CloudOfficeToken(currentUserId, typeof(TCloudEvent).FullName,tokenType)))
                .SynchronizeLocalEvent(objectSpaceFactory, localEventType));

        public static CloudOfficeToken CloudOfficeToken(this IObjectSpace objectSpace,Guid currentUserId,string entityName,string tokenType){
            var cloudOfficeToken = objectSpace.GetObjectsQuery<CloudOfficeToken>().FirstOrDefault(token =>
                token.CloudOfficeTokenStorage.Oid == currentUserId && token.EntityName == entityName&&token.TokenType==tokenType);
            
            if (cloudOfficeToken == null){
                cloudOfficeToken = objectSpace.CreateObject<CloudOfficeToken>();
                cloudOfficeToken.CloudOfficeTokenStorage =objectSpace.GetObjectByKey<CloudOfficeTokenStorage>(currentUserId)?? objectSpace.CreateObject<CloudOfficeTokenStorage>();
                cloudOfficeToken.CloudOfficeTokenStorage.Oid = currentUserId;
                cloudOfficeToken.TokenType = tokenType;
                cloudOfficeToken.EntityName=entityName;
            }
            
            objectSpace.CommitChanges();
            return cloudOfficeToken;
        }

        static IObservable<(IObjectSpace objectSpace, IEvent source, TCloudEvent @event, MapAction mapAction)>
            SynchronizeLocalEvent<TCloudEvent>(this IObservable<(TCloudEvent @event, MapAction mapAction)> source,
                Func<IObjectSpace> objectSpaceFactory, Type localEventType) 
            => source.SelectMany(t => Observable.Using(objectSpaceFactory, objectSpace => objectSpace
                .QueryCloudOfficeObject((string)t.@event.GetPropertyValue("Id"), t.@event.GetType().ToCloudObjectType()).ToObservable(Scheduler.Immediate)
                .Select(cloudObject => ((IEvent) objectSpace.GetObjectByKey(localEventType,new Guid(cloudObject.LocalId)))).Pair(t)
                .Select(tuple => (objectSpace,tuple.source,tuple.other.@event,tuple.other.mapAction))
                .SwitchIfEmpty((objectSpace,(IEvent)null,t.@event,t.mapAction).Observe().Select(tuple => tuple))
                .Finally(objectSpace.CommitChanges)));

        public static IObservable<(TCloudEntity serviceObject, MapAction mapAction)>
            SynchronizeCloud<TCloudEntity, TLocalEntity>(this Func<IObjectSpace> objectSpaceFactory, SynchronizationType synchronizationType, IObjectSpace objectSpace,
            Func<string, IObservable<Unit>> deleteRequest, Func<TCloudEntity, IObservable<TCloudEntity>> insertRequest, Func<(string cloudId,MapAction mapAction), IObservable<TCloudEntity>> getRequest,
            Func<(TCloudEntity cloudEntity, TLocalEntity localEntity, string cloudId), IObservable<TCloudEntity>> updateRequest, 
            Action<GenericEventArgs<(CloudOfficeObject cloudOfficeObject, TLocalEntity localEntinty)>> onDelete = null, Action<(TCloudEntity target, TLocalEntity source)> onInsert = null,
            Action<(TCloudEntity target, TLocalEntity source)> update = null) where TCloudEntity : class where TLocalEntity : class 
            => objectSpace.ModifiedObjects<TLocalEntity, TCloudEntity>(synchronizationType, cloudOfficeObject 
                    => cloudOfficeObject.Delete<TCloudEntity,TLocalEntity>(onDelete, deleteRequest).FirstOrDefaultAsync().Select(entity => (entity,MapAction.Delete)),
                localEntity => objectSpaceFactory.MapEntity(localEntity, sourceEntity 
                        => sourceEntity.Insert(objectSpace, objectSpaceFactory, onInsert, insertRequest).Select(entity => (entity, MapAction.Insert)), _ 
                        => _.task.Update(_.cloudId, getRequest, updateRequest, update).Select(entity => (entity, MapAction.Update))
                ));

        private static IObservable<TCloudEntity> Update<TCloudEntity, TLocalEntity>(this TLocalEntity source, string cloudId, Func<(string cloudId,MapAction mapAction), IObservable<TCloudEntity>> getRequest,
	        Func<(TCloudEntity cloudEntity, TLocalEntity localEntity, string cloudId), IObservable<TCloudEntity>> updateRequest, 
	        Action<(TCloudEntity target, TLocalEntity source)> update = null) 
            => getRequest((cloudId,MapAction.Update)).SelectMany(target => {
                    update?.Invoke((target, source));
			        return updateRequest.Start(source, cloudId,  target);
		        });

        private static IObservable<TCloudEntity> Start<TCloudEntity, TLocalEntity>(this Func<(TCloudEntity cloudEntity, TLocalEntity localEntity, string cloudId), 
            IObservable<TCloudEntity>> updateRequest,TLocalEntity source, string cloudId,  TCloudEntity target) 
            => AppDomain.CurrentDomain.IsHosted() ? Observable.Start(() 
                => updateRequest((target, source, cloudId)).Wait()) : updateRequest((target, source, cloudId));

        private static IObservable<TCloudEntity> Insert<TCloudEntity, TLocalEntity>(this TLocalEntity sourceEvent, IObjectSpace objectSpace, Func<IObjectSpace> objectSpaceFactory,
            Action<(TCloudEntity, TLocalEntity)> insert, Func<TCloudEntity, IObservable<TCloudEntity>> insertRequest){
            var cloudEntity = (TCloudEntity)typeof(TCloudEntity).CreateInstance();
            insert?.Invoke((cloudEntity, sourceEvent));
            return insertRequest.Start(cloudEntity)
                .NewCloudObject(objectSpaceFactory, objectSpace.GetKeyValue(sourceEvent).ToString());
        }

        private static IObservable<TCloudEntity> Start<TCloudEntity>(this Func<TCloudEntity, IObservable<TCloudEntity>> insertRequest, TCloudEntity updatedEvent) 
            => AppDomain.CurrentDomain.IsHosted()? Observable.Start(() 
                => insertRequest(updatedEvent).Wait()):insertRequest(updatedEvent);

        private static IObservable<TCloudEntity> Delete<TCloudEntity, TLocalEntity>(this (CloudOfficeObject cloudOfficeObject, TLocalEntity localEntinty) t,
            Action<GenericEventArgs<(CloudOfficeObject cloudOfficeObject, TLocalEntity localEntinty)>> delete,
            Func<string, IObservable<Unit>> deleteRequest) where TCloudEntity : class{
            var args = new GenericEventArgs<(CloudOfficeObject cloudOfficeObject,TLocalEntity localEntinty)>(t);
            delete?.Invoke(args);
            return !args.Handled ? deleteRequest.Start<TCloudEntity>(t.cloudOfficeObject) : Observable.Empty<TCloudEntity>();
        }

        private static IObservable<TCloudEntity> Start<TCloudEntity>(this Func<string, IObservable<Unit>> deleteRequest,CloudOfficeObject cloudOfficeObject) where TCloudEntity : class 
            => AppDomain.CurrentDomain.IsHosted()? Observable.Start(() 
                    => deleteRequest(cloudOfficeObject.CloudId).Select(_ => (TCloudEntity)null).Wait())
                :deleteRequest(cloudOfficeObject.CloudId).Select(_ => (TCloudEntity)null);
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