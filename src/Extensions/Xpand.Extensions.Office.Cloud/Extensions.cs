using System;
using System.Drawing;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Templates;
using Fasterflect;

using Xpand.Extensions.Office.Cloud.BusinessObjects;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.Extensions.Office.Cloud{
    public static class Extensions{
        public static void SaveToken(this ICloudOfficeToken store, Func<IObjectSpace> objectSpaceFactory){
            using var space = objectSpaceFactory();
            var storage = (ICloudOfficeToken)(space.GetObject(store) ?? space.CreateObject(store.GetType()));
            storage.Token = store.Token;
            storage.TokenType = store.TokenType;
            storage.EntityName = store.EntityName;
            space.CommitChanges();
        }

        internal static IObservable<TCloudEntity> MapEntity<TCloudEntity, TLocalEntity>(this Func<IObjectSpace> objectSpaceFactory, TLocalEntity localEntity,
            Func<TLocalEntity, IObservable<TCloudEntity>> insert, Func<(string cloudId, TLocalEntity task), IObservable<TCloudEntity>> update){
            var objectSpace = objectSpaceFactory();
            var localId = objectSpace.GetKeyValue(localEntity).ToString();
            var cloudId = objectSpace.GetCloudId(localId, localEntity.GetType());
            return cloudId == null ? insert(localEntity) : update((cloudId, localEntity));
        }

        
        
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
                        .Select(officeObject => (officeObject,bo:o));
                }))
                .SelectMany(t => delete(t).Select(_ => t.officeObject))
                .To((TCloudEntity)typeof(TCloudEntity).CreateInstance())
                .Select(o => (o,MapAction.Delete)):Observable.Empty<(TCloudEntity serviceObject, MapAction mapAction)>();

            var newObjects = synchronizationType.IsCreate() ? objectSpace.WhenCommiting<TLocalEntity>(ObjectModification.New,true)
                .SelectMany(_ => _.objects).SelectMany(map):Observable.Empty<(TCloudEntity serviceObject, MapAction mapAction)>();
            
            var updateObjects = synchronizationType.IsUpdate() ? objectSpace.WhenCommiting<TLocalEntity>(ObjectModification.Updated,true)
                .SelectMany(_ => _.objects).SelectMany(map):Observable.Empty<(TCloudEntity serviceObject, MapAction mapAction)>();
            return updateObjects.Merge(newObjects).Merge(deleteObjects);
        }

        public static IObservable<TServiceObject> MapEntities<TBO, TServiceObject>(this IObjectSpace objectSpace,IObservable<TBO> deletedObjects,
            IObservable<TBO> newOrUpdatedObjects, Func<CloudOfficeObject, IObservable<TServiceObject>> delete, Func<TBO, IObservable<TServiceObject>> map) 
            => newOrUpdatedObjects.SelectMany(map)
                .Merge(deletedObjects
                    .SelectMany(_ => {
                        var deletedId = objectSpace.GetKeyValue(_).ToString();
                        return objectSpace.QueryCloudOfficeObject(typeof(TServiceObject), _).Where(o => o.LocalId == deletedId).ToObservable();
                    })
                    .SelectMany(cloudOfficeObject => delete(cloudOfficeObject).Select(_ => cloudOfficeObject))
                    .To<TServiceObject>());

        public static IObservable<T> NewCloudObject<T>(this IObservable<T> source, Func<IObjectSpace> objectSpaceFactory, string localId) 
            => source.SelectMany(@event => Observable.Using(objectSpaceFactory, 
                space => space.NewCloudObject(localId, (string)@event.GetPropertyValue("Id"), @event.GetType().ToCloudObjectType())
                    .Select(_ => @event)));
        
        public static IObservable<CloudOfficeObject> NewCloudObject(this IObjectSpace space, string localId, string cloudId, Type cloudObjectType) 
            => space.NewCloudObject(localId, cloudId, cloudObjectType.ToCloudObjectType());

        public static IObservable<CloudOfficeObject> NewCloudObject(this IObjectSpace space, object localEntity, object cloudEntity){
            var localId = space .GetKeyValue(localEntity).ToString();
            var cloudId = cloudEntity.GetPropertyValue("Id").ToString();
            return space.NewCloudObject(localId, cloudId, cloudEntity.GetType().ToCloudObjectType());
        }

        
        public static IObservable<CloudOfficeObject> NewCloudObject(this IObjectSpace space, string localId, string cloudId, CloudObjectType cloudObjectType){
            var cloudObject = space.CreateObject<CloudOfficeObject>();
            cloudObject.LocalId = localId;
            cloudObject.CloudId = cloudId;
            cloudObject.CloudObjectType = cloudObjectType;
            space.CommitChanges();
            return cloudObject.ReturnObservable();
        }

        
        public static string GetCloudId(this IObjectSpace objectSpace, string localId, Type cloudEntityType) 
            => objectSpace.QueryCloudOfficeObject(localId, cloudEntityType).FirstOrDefault()?.CloudId;

        private static IObservable<SimpleAction> RegisterAuthActions(this ApplicationModulesManager manager,string serviceName) 
            => manager.RegisterViewSimpleAction($"Connect{serviceName}", action => action.Initialize(serviceName))
                .Merge(manager.RegisterViewSimpleAction($"Disconnect{serviceName}", action => action.Initialize(serviceName)))
                .Publish().RefCount();

        private static void Initialize(this SimpleAction action,string serviceName){
            action.Caption = $"Sign In {serviceName}";
            action.ImageName = serviceName;
            if (action.Id == $"Connect{serviceName}"){
                action.ToolTip = "Connect";
            }
            else{
                action.Caption = $"Sign out {serviceName}";
                action.ToolTip="Sign out";
            }
            action.PaintStyle=ActionItemPaintStyle.CaptionAndImage;

        }
    
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager, string serviceName,
            Type serviceStorageType, Func<XafApplication, IObservable<bool>> needsAuthentication, Func<XafApplication, IObservable<Unit>> authorize){
            var registerActions = manager.RegisterAuthActions(serviceName);
            return registerActions.ConfigureStyle()
                .Merge(registerActions.ExecuteActions(needsAuthentication,serviceName,serviceStorageType,authorize))
                .ToUnit();
        }

        public static IObservable<bool> NeedsAuthentication<TAuthentication>(this XafApplication application,Func<IObservable<bool>> authorize) where TAuthentication:CloudOfficeBaseObject 
            => application.UseObjectSpace(space => (space.GetObjectByKey<TAuthentication>( application.CurrentUserId()) == null).ReturnObservable())
                .SelectMany(b => !b ? authorize() : true.ReturnObservable());

        private static IObservable<Unit> ConfigureStyle(this IObservable<SimpleAction> source) 
            => source.WhenCustomizeControl()
                .Select(_ => {
                    var application = _.sender.Application;
                    if (application.GetPlatform() == Platform.Web) {
                        if (_.sender.Id.StartsWith("Connect")) {
                            _.sender.Model.SetValue("IsPostBackRequired", true);
                        }

                        var menuItem = _.e.Control.GetPropertyValue("MenuItem");
                        var itemStyle = menuItem.GetPropertyValue("ItemStyle");
                        itemStyle.GetPropertyValue("Font").SetPropertyValue("Name", "Roboto Medium");
                        itemStyle.GetPropertyValue("SelectedStyle").SetPropertyValue("BackColor", Color.White);
                        itemStyle.SetPropertyValue("ForeColor", ColorTranslator.FromHtml("#757575"));
                        itemStyle.GetPropertyValue("HoverStyle").SetPropertyValue("BackColor", Color.White);
                        menuItem.CallMethod("ForceMenuRendering");
                    }

                    return _.sender;
                })
                .ToUnit();

        private static IObservable<SimpleAction> Activate(this IObservable<(bool needsAuth, SimpleAction action)> source, string serviceName, Type serviceStorageType) 
            => source.Select(t => {
                    t.action.Activate(nameof(NeedsAuthentication), t.action.Id.StartsWith("Connect") ? t.needsAuth : !t.needsAuth);
                    if (!t.needsAuth && t.action.Id.StartsWith("Disconnect")){
                        t.action.UpdateDisconnectActionToolTip(serviceName,serviceStorageType);
                    }

                    return t.action;
                });

        private static void UpdateDisconnectActionToolTip(this SimpleAction action, string serviceName, Type serviceStorageType){
            using var objectSpace = action.Application.CreateObjectSpace(serviceStorageType);
            var disconnectMicrosoft = action.Controller.Frame.Actions().First(a => a.Id==$"Disconnect{serviceName}");
            var currentUserId = action.Application.CurrentUserId();
            var objectByKey = objectSpace.GetObjectByKey(serviceStorageType,currentUserId);
            var userName = objectByKey?.GetPropertyValue("UserName");
            if (!disconnectMicrosoft.Data.ContainsKey("ToolTip")){
                disconnectMicrosoft.Data["ToolTip"] = disconnectMicrosoft.ToolTip;
            }
            disconnectMicrosoft.ToolTip = $"{disconnectMicrosoft.Data["ToolTip"]} {userName}";
        }

        private static IObservable<Unit> ExecuteActions(this IObservable<SimpleAction> registerActions,
            Func<XafApplication, IObservable<bool>> needsAuthentication, string serviceName, Type serviceStorageType,
            Func<XafApplication, IObservable<Unit>> authorize) 
            => registerActions.ActivateWhenUserDetails()
                .SelectMany(action => needsAuthentication(action.Application).Select(b => b).Pair(action)
                    .Activate(serviceName, serviceStorageType)
                    .Merge(action.Authorize(serviceName,serviceStorageType,authorize,needsAuthentication))
                ).ToUnit();

        private static IObservable<SimpleAction> Authorize(this SimpleAction action, string serviceName,
            Type serviceStorageType, Func<XafApplication, IObservable<Unit>> authorize,
            Func<XafApplication, IObservable<bool>> needsAuthentication) 
            => action.WhenExecute(e => {
                var execute = e.Action.Id == $"Disconnect{serviceName}"
                    ? e.Action.Application.UseObjectSpace(_ => {
                        var objectSpace = e.Action.View().ObjectSpace;
                        objectSpace.Delete(objectSpace.GetObjectByKey(serviceStorageType,e.Action.Application.CurrentUserId()));
                        objectSpace.CommitChanges();
                        e.Action.Data.Clear();
                        return e.Action.AsSimpleAction().ReturnObservable();
                    })
                    : authorize(e.Action.Application).To(e.Action.AsSimpleAction());
                return execute.SelectMany(simpleAction => needsAuthentication(simpleAction.Application).Pair(simpleAction))
                    .ActivateWhenAuthenticationNeeded(serviceName, serviceStorageType);
            });

        private static IObservable<SimpleAction> ActivateWhenUserDetails(this IObservable<SimpleAction> registerActions) 
            => registerActions.Select(action => action).ActivateInUserDetails()
                .Do(action => action.Activate(nameof(NeedsAuthentication),false) );

        
        private static IObservable<SimpleAction> ActivateWhenAuthenticationNeeded(
            this IObservable<(bool needsAuth, SimpleAction action)> source, string serviceName, Type serviceStorageType) 
            => source.Select(t => {
                    if (t.action.Id == $"Connect{serviceName}"){
                        t.action.Activate(nameof(NeedsAuthentication), t.needsAuth);
                        t.action.Controller.Frame.Action($"Disconnect{serviceName}").Activate(nameof(NeedsAuthentication), !t.needsAuth);
                    }
                    else{
                        t.action.Activate(nameof(NeedsAuthentication), !t.needsAuth);
                        t.action.Controller.Frame.Action($"Connect{serviceName}").Activate(nameof(NeedsAuthentication), t.needsAuth);
                    }
                    t.action.UpdateDisconnectActionToolTip(serviceName, serviceStorageType);
                    return t.action;
                })
                .WhenActive();


    }


    
}