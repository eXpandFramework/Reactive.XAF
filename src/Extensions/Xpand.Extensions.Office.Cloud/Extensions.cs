using System;

using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Fasterflect;
using JetBrains.Annotations;
using Xpand.Extensions.Office.Cloud.BusinessObjects;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.Extensions.Office.Cloud{
    public static class Extensions{
        public static void SaveToken(this ITokenStore store, Func<IObjectSpace> objectSpaceFactory){
            using (var space = objectSpaceFactory()){
                var storage = (ITokenStore)(space.GetObject(store) ?? space.CreateObject(store.GetType()));
                storage.Token = store.Token;
                storage.TokenType = store.TokenType;
                storage.EntityName = store.EntityName;
                space.CommitChanges();
            }
        }

        internal static IObservable<TCloudEntity> MapEntity<TCloudEntity, TLocalEntity>(this Func<IObjectSpace> objectSpaceFactory, TLocalEntity localEntity,
            Func<TLocalEntity, IObservable<TCloudEntity>> insert, Func<(string cloudId, TLocalEntity task), IObservable<TCloudEntity>> update){
            var objectSpace = objectSpaceFactory();
            var localId = objectSpace.GetKeyValue(localEntity).ToString();
            var cloudId = objectSpace.GetCloudId(localId, localEntity.GetType());
            return cloudId == null ? insert(localEntity) : update((cloudId, localEntity));
        }

        
        [PublicAPI]
        public static IObservable<T> DeleteObjectSpaceLink<T>(this IObservable<T> source) where T : IObjectSpaceLink 
            => source.Select(link => {
                link.ObjectSpace.Delete(link);
                link.ObjectSpace.CommitChanges();
                return link;
            });

        public static bool IsDelete(this SynchronizationType synchronizationType) 
            => new[]{SynchronizationType.CreatedOrDeleted,SynchronizationType.Deleted,SynchronizationType.All }.Contains(synchronizationType);

        public static bool IsCreate(this SynchronizationType synchronizationType) 
            => new[]{SynchronizationType.Created, SynchronizationType.CreatedOrDeleted, SynchronizationType.CreatedOrUpdated,SynchronizationType.All}.Contains(synchronizationType);
        
        public static bool IsUpdate(this SynchronizationType synchronizationType) 
            => new[]{SynchronizationType.Updated, SynchronizationType.CreatedOrUpdated, SynchronizationType.UpdatedOrDeleted,SynchronizationType.All}.Contains(synchronizationType);

        public static IObservable<(TCloudEntity serviceObject, MapAction mapAction)>
            ModifiedObjects<TLocalEntity, TCloudEntity>(this IObjectSpace objectSpace, SynchronizationType synchronizationType, 
                Func<(CloudOfficeObject cloudOfficeObject, TLocalEntity localEntity), IObservable<(TCloudEntity , MapAction mapAction)>> delete,
                Func<TLocalEntity, IObservable<(TCloudEntity serviceObject, MapAction mapAction)>> map){
            
            var deleteObjects =synchronizationType.IsDelete()? objectSpace.WhenDeletedObjects<TLocalEntity>(true)
                .SelectMany(_ => _.objects.SelectMany(o => {
	                var deletedId = _.objectSpace.GetKeyValue(o).ToString();
	                return _.objectSpace.QueryCloudOfficeObject(typeof(TCloudEntity), o).Where(officeObject => officeObject.LocalId == deletedId).ToArray()
                        .Select(officeObject => (officeObject,bo:(TLocalEntity)o));
                }))
                .SelectMany(t => delete(t).Select(s => t.officeObject))
                // .DeleteObjectSpaceLink()
                .To((TCloudEntity)typeof(TCloudEntity).CreateInstance())
                .Select(o => (o,MapAction.Delete)):Observable.Empty<(TCloudEntity serviceObject, MapAction mapAction)>();

            var newObjects = synchronizationType.IsCreate() ? objectSpace.WhenModifiedObjects<TLocalEntity>(true, ObjectModification.New)
                .SelectMany(_ => _.objects).Cast<TLocalEntity>().SelectMany(map):Observable.Empty<(TCloudEntity serviceObject, MapAction mapAction)>();
            
            var updateObjects = synchronizationType.IsUpdate() ? objectSpace.WhenModifiedObjects<TLocalEntity>(true, ObjectModification.Updated)
                .SelectMany(_ => _.objects).Cast<TLocalEntity>().SelectMany(map):Observable.Empty<(TCloudEntity serviceObject, MapAction mapAction)>();
            return updateObjects.Merge(newObjects).Merge(deleteObjects);
        }
        public static IObservable<TServiceObject> MapEntities<TBO, TServiceObject>(this IObjectSpace objectSpace,IObservable<TBO> deletedObjects,
            IObservable<TBO> newOrUpdatedObjects, Func<CloudOfficeObject, IObservable<TServiceObject>> delete, Func<TBO, IObservable<TServiceObject>> map){
            return newOrUpdatedObjects.SelectMany(map)
                .Merge(deletedObjects
                    .SelectMany(_ => {
                        var deletedId = objectSpace.GetKeyValue(_).ToString();
                        return objectSpace.QueryCloudOfficeObject(typeof(TServiceObject), _).Where(o => o.LocalId == deletedId).ToObservable();
                    })
                    // .DeleteObjectSpaceLink()
                    .SelectMany(cloudOfficeObject => delete(cloudOfficeObject).Select(s => cloudOfficeObject))
                    .To<TServiceObject>());
        }

        public static IObservable<T> NewCloudObject<T>(this IObservable<T> source, Func<IObjectSpace> objectSpaceFactory, string localId) => source
            .SelectMany(@event => Observable.Using(objectSpaceFactory, 
                space => space.NewCloudObject(localId, (string)@event.GetPropertyValue("Id"), @event.GetType().ToCloudObjectType())
                    .Select(unit => @event)));
        [PublicAPI]
        public static IObservable<CloudOfficeObject> NewCloudObject(this IObjectSpace space, string localId, string cloudId, Type cloudObjectType) => space
            .NewCloudObject(localId, cloudId, cloudObjectType.ToCloudObjectType());

        public static IObservable<CloudOfficeObject> NewCloudObject(this IObjectSpace space, object localEntity, object cloudEntity){
            var localId = space .GetKeyValue(localEntity).ToString();
            var cloudId = cloudEntity.GetPropertyValue("Id").ToString();
            return space.NewCloudObject(localId, cloudId, cloudEntity.GetType().ToCloudObjectType());
        }
        [PublicAPI]
        public static IObservable<CloudOfficeObject> NewCloudObject(this IObjectSpace space, string localId, string cloudId, CloudObjectType cloudObjectType){
            var cloudObject = space.CreateObject<CloudOfficeObject>();
            cloudObject.LocalId = localId;
            cloudObject.CloudId = cloudId;
            cloudObject.CloudObjectType = cloudObjectType;
            space.CommitChanges();
            return cloudObject.ReturnObservable();
        }
        [PublicAPI]
        public static string GetCloudId(this IObjectSpace objectSpace, string localId, Type cloudEntityType) =>
            objectSpace.QueryCloudOfficeObject(localId, cloudEntityType).FirstOrDefault()?.CloudId;
        
    }


    
}