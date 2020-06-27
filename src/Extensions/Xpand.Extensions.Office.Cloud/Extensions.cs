using System;

using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Fasterflect;
using JetBrains.Annotations;
using Xpand.Extensions.Office.Cloud.BusinessObjects;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.Extensions.Office.Cloud{
    public static class Extensions{
        public static void SaveToken(this ITokenStore store, Func<IObjectSpace> objectSpaceFactory){
            using (var space = objectSpaceFactory()){
                var storage = (ITokenStore)(space.GetObject(store) ?? space.CreateObject(store.GetType()));
                storage.Token = store.Token;
                storage.EntityName = store.EntityName;
                space.CommitChanges();
            }
        }

        static Extensions() => AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
        private static System.Reflection.Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args){
            if (args.Name.Contains("Newton")){
                return System.Reflection.Assembly.LoadFile($@"{AppDomain.CurrentDomain.BaseDirectory}bin\Newtonsoft.Json.dll");
            }
            return null;
        }

        public static IObservable<TCloudEntity> MapEntity<TCloudEntity, TLocalEntity>(this Func<IObjectSpace> objectSpaceFactory, TLocalEntity localEntity,
            Func<TLocalEntity, IObservable<TCloudEntity>> insert, Func<(string cloudId, TLocalEntity task), IObservable<TCloudEntity>> update){
            var objectSpace = objectSpaceFactory();
            var localId = objectSpace.GetKeyValue(localEntity).ToString();
            var cloudId = objectSpace.GetCloudId(localId, localEntity.GetType());
            return cloudId == null ? insert(localEntity) : update((cloudId, localEntity));
        }

        
        [PublicAPI]
        public static IObservable<T> DeleteObjectSpaceLink<T>(this IObservable<T> source) where T : IObjectSpaceLink => source.Select(link => {
            link.ObjectSpace.Delete(link);
            link.ObjectSpace.CommitChanges();
            return link;
        });

        public static IObservable<TServiceObject> MapEntities<TBO, TServiceObject>(this IObjectSpace objectSpace,
            Func<CloudOfficeObject, IObservable<TServiceObject>> delete, Func<TBO, IObservable<TServiceObject>> map){
            var deleteObjects = objectSpace.WhenDeletedObjects<TBO>(true)
                .SelectMany(_ => _.objects.SelectMany(o => {
	                var deletedId = _.objectSpace.GetKeyValue(o).ToString();
	                return _.objectSpace.QueryCloudOfficeObject(typeof(TServiceObject), o).Where(officeObject => officeObject.LocalId == deletedId);
                }))
                .DeleteObjectSpaceLink()
                .SelectMany(cloudOfficeObject => delete(cloudOfficeObject).Select(s => cloudOfficeObject))
                .To((TServiceObject)typeof(TServiceObject).CreateInstance());
            var mapObjects = objectSpace.WhenModifiedObjects<TBO>(true, ObjectModification.NewOrUpdated)
                .SelectMany(_ => _.objects).Cast<TBO>().SelectMany(map);
            return mapObjects.Merge(deleteObjects);
        }
        public static IObservable<TServiceObject> MapEntities<TBO, TServiceObject>(this IObjectSpace objectSpace,IObservable<TBO> deletedObjects,
            IObservable<TBO> newOrUpdatedObjects, Func<CloudOfficeObject, IObservable<TServiceObject>> delete, Func<TBO, IObservable<TServiceObject>> map){
            return newOrUpdatedObjects.SelectMany(map)
                .Merge(deletedObjects
                    .SelectMany(_ => {
                        var deletedId = objectSpace.GetKeyValue(_).ToString();
                        return objectSpace.QueryCloudOfficeObject(typeof(TServiceObject), _).Where(o => o.LocalId == deletedId).ToObservable();
                    })
                    .DeleteObjectSpaceLink()
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
            var localId = space.GetKeyValue(localEntity).ToString();
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

        public static IObservable<Unit> ConfigureCloudAction<TAuthentication>(this IObservable<SimpleAction> source, string disconnectImageName, string connectImageName,
            Func<SimpleAction, IObservable<Unit>> aquireAuthorization, Func<SimpleAction, IObservable<Unit>> validateAuthorization=null) where TAuthentication : CloudOfficeBaseObject =>
            
            source.SelectMany(action => (validateAuthorization ??= aquireAuthorization)(action).To(action))
                .SelectMany(action => action.WhenExecute()
                    .SelectMany(_=> (_.Action.ImageName == connectImageName ? aquireAuthorization(_.Action.AsSimpleAction())
                        : _.Action.RemoveAuthorization<TAuthentication>()).To(_.Action.AsSimpleAction()))
                    .SelectMany(simpleAction => (validateAuthorization ??= aquireAuthorization)?.Invoke(simpleAction).To(simpleAction)))
                .ToUnit();
        
        private static IObservable<Unit> RemoveAuthorization<TAuthentication>(this ActionBase action) where TAuthentication : CloudOfficeBaseObject =>
	        action.Application.NewObjectSpace(space => {
                    var authentication = space.GetObjectByKey<TAuthentication>(SecuritySystem.CurrentUserId);
                    space.Delete(authentication);
                    space.CommitChanges();
                    return Unit.Default.ReturnObservable();
                })
                .ToUnit();
    }


    
}