using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Shouldly;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Office.Cloud.BusinessObjects;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Office.Cloud.Tests{
	public static class TestExtensions{
        
        public static async Task Populate_All<TEntity>(this IObjectSpace objectSpace, string syncToken,
            Func<ICloudOfficeToken,IObservable<TEntity>> listEvents,TimeSpan timeout,Action<IObservable<TEntity>> assert,IObjectSpaceProvider assertTokenStorageProvider=null){
            
            var tokenStorage = objectSpace.CreateObject<CloudOfficeToken>();
            tokenStorage.Token = syncToken;
            tokenStorage.EntityName = typeof(TEntity).FullName;
            objectSpace.CommitChanges();
            var storageOid = tokenStorage.Oid;
            var events = listEvents(tokenStorage).SubscribeReplay();
                
            await events.Timeout(timeout);

            if (assertTokenStorageProvider!=null){
                using var space = assertTokenStorageProvider.CreateObjectSpace();
                tokenStorage = space.GetObjectsQuery<CloudOfficeToken>()
                    .First(storage => storage.Oid == storageOid);
                tokenStorage.Token.ShouldNotBeNull();
                tokenStorage.Token.ShouldNotBe(syncToken);
            }

            assert(events);

        }

        public static async Task Delete_Two_Entities<TCloudEntity,TLocalEntity>(this IObjectSpace objectSpace,
            TLocalEntity[] localEntites, Func<IObjectSpace, IObservable<TCloudEntity>> synchronize,
            Func<Task> assert,TimeSpan timeout,TCloudEntity[] existingEntities){
            
            var localEntity1 = objectSpace.GetObject(localEntites[0]);
            var cloudEntity1 = existingEntities.First();
            await objectSpace.NewCloudObject(localEntity1, cloudEntity1);
            var localEntity2 = objectSpace.GetObject(localEntites[1]);
            var cloudEntity2 = existingEntities.Skip(1).First();
            await objectSpace.NewCloudObject(localEntity2, cloudEntity2);

            var map = synchronize(objectSpace).SubscribeReplay();
            objectSpace.Delete(localEntity1);
            objectSpace.CommitChanges();
            await Task.Delay(TimeSpan.FromSeconds(3));
            objectSpace.Delete(localEntity2);
            objectSpace.CommitChanges();

            
            await map.Take(2).LastAsync().Delay(TimeSpan.FromSeconds(3)).SelectMany((_, _) => Observable.FromAsync(assert)).Timeout(timeout);

        }

        public static async Task Map_Existing_Entity_Two_Times<TCloudEntity, TLocalEntity>(this IObjectSpace objectSpace, TLocalEntity localEntity,
            Action<TLocalEntity, int> modifyLocalEntity, TCloudEntity newCloudEntity, Func<IObjectSpace, IObservable<TCloudEntity>> synchronize,
            Func<TLocalEntity, TCloudEntity,Task> assert, TimeSpan timeout) {
            
            
            localEntity = objectSpace.GetObject(localEntity);
            
            await objectSpace.NewCloudObject(localEntity, newCloudEntity).ToTaskWithoutConfigureAwait();
            var map = synchronize(objectSpace).SubscribePublish();

            
            modifyLocalEntity(localEntity,0);
            objectSpace.CommitChanges();
            
            await map.Take(1).SelectMany((cloudEntity, _) => assert( localEntity, cloudEntity).ToObservable())
                .Timeout(timeout).ToTaskWithoutConfigureAwait();
            
            modifyLocalEntity(localEntity,1);
            objectSpace.CommitChanges();
            
            await map.Take(1).Select((cloudEntity, _) => {
                    assert( localEntity, cloudEntity);
                    return Unit.Default;
                })
                .Timeout(timeout);
        }

        public static async Task Populate_Modified<TEntity>(this IObjectSpaceProvider objectSpaceProvider,
            Func<ICloudOfficeToken,IObservable<TEntity>> listEntities,IObservable<Unit> modified,TimeSpan timeout,Action<IObservable<TEntity>> assert){
            using var objectSpace = objectSpaceProvider.CreateObjectSpace();
            var tokenStorage = objectSpace.CreateObject<CloudOfficeToken>();
            var storageToken = tokenStorage.Token;
            await Observable.Start(() => listEntities(tokenStorage).ToTask()).Merge().ToTaskWithoutConfigureAwait();

            await modified.Timeout(timeout);

            var entities = listEntities(tokenStorage).SubscribeReplay();
            await entities.Timeout(timeout);

            tokenStorage.Token.ShouldNotBeNull();
            tokenStorage.Token.ShouldNotBe(storageToken);
                
            assert(entities);
        }

        public static async Task Map_Two_New_Entity<TCloudEntity,TLocalEntity>(this IObjectSpace objectSpace,Func<IObjectSpace,int,TLocalEntity> localEntityFactory,TimeSpan timeout,
            Func<IObjectSpace, IObservable<TCloudEntity>> synchronize, Action<TLocalEntity,TCloudEntity,int> assert){
            
            var map = synchronize(objectSpace).Take(2).SubscribeReplay();
            var localEntity1 = localEntityFactory(objectSpace,0);
            objectSpace.CommitChanges();
            
            await map.FirstAsync().Select((cloudEntity, _) => {
                    assert( localEntity1, cloudEntity,0);
                    return Unit.Default;
                })
                .TakeUntil(objectSpace.WhenDisposed())
                .Timeout(timeout);
            
            var localEntity2 = localEntityFactory(objectSpace,1);
            objectSpace.CommitChanges();

            await map.LastAsync().Select((cloudEntity, _) => {
                    assert( localEntity2, cloudEntity,1);
                    return Unit.Default;
                })
                .TakeUntil(objectSpace.WhenDisposed())
                .Timeout(timeout);
        }
        
        public static void NewAuthentication<TAuth>(this IObjectSpaceProvider objectSpaceProvider,Action<TAuth,byte[]> saveToken,string serviceName,Platform platform=Platform.Win) where TAuth:CloudOfficeBaseObject{
            using var manifestResourceStream = File.OpenRead($"{AppDomain.CurrentDomain.ApplicationPath()}\\{serviceName}AuthenticationData{platform}.json");
            var token = Encoding.UTF8.GetBytes(new StreamReader(manifestResourceStream).ReadToEnd());
            using var objectSpace = objectSpaceProvider.CreateObjectSpace();
            var authenticationOid = (Guid)objectSpace.GetKeyValue(SecuritySystem.CurrentUser);
            if (objectSpace.GetObjectByKey<TAuth>(authenticationOid)==null){
                var authentication = objectSpace.CreateObject<TAuth>();
                authentication.Oid=authenticationOid;
                saveToken(authentication, token);
                objectSpace.CommitChanges();
            }
        }
        
        public static async Task Disconnect_Action_Destroys_Connection(this XafApplication application, string serviceName){
            
            var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
            compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
            var viewWindow = application.CreateViewWindow();
            viewWindow.SetView(compositeView);
            var disconnectMicrosoft = viewWindow.DisconnectAction(serviceName);
            if (!disconnectMicrosoft.Active){
                await disconnectMicrosoft.WhenActivated().FirstAsync().ToTaskWithoutConfigureAwait();
            }
            disconnectMicrosoft.DoExecute();
                
            disconnectMicrosoft.Active[nameof(Extensions.Office.Cloud.Extensions.NeedsAuthentication)].ShouldBeFalse();
            viewWindow.ConnectAction(serviceName).Active[nameof(Extensions.Office.Cloud.Extensions.NeedsAuthentication)].ShouldBeTrue();
        }

        public static async Task Actions_Active_State_when_authentication_not_needed(this XafApplication application, string serviceName){
            var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
            compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
            var viewWindow = application.CreateViewWindow();
                
            viewWindow.SetView(compositeView);

            await ActiveState(viewWindow,false, serviceName).ConfigureAwait(false);
        }
        
        private static async Task ActiveState(Window viewWindow, bool authenticationNeeded, string serviceName){
            
            await Observable.Interval(TimeSpan.FromMilliseconds(200))
                .Where(_ => viewWindow.ConnectAction(serviceName).Active[nameof(Extensions.Office.Cloud.Extensions.NeedsAuthentication)]==authenticationNeeded)
                .FirstAsync()
                .ToTaskWithoutConfigureAwait();
            await Observable.Interval(TimeSpan.FromMilliseconds(200))
                .Where(_ => viewWindow.DisconnectAction(serviceName).Active[nameof(Extensions.Office.Cloud.Extensions.NeedsAuthentication)]=!authenticationNeeded)
                .FirstAsync()
                .ToTaskWithoutConfigureAwait();
        }


        public static void Actions_Active_State_when_authentication_needed(this XafApplication application, string serviceName){
            var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
            compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
            var viewWindow = application.CreateViewWindow();
                
            viewWindow.SetView(compositeView);

            viewWindow.ConnectAction(serviceName).Active[nameof(Extensions.Office.Cloud.Extensions.NeedsAuthentication)].ShouldBeTrue();
            viewWindow.DisconnectAction(serviceName).Active[nameof(Extensions.Office.Cloud.Extensions.NeedsAuthentication)].ShouldBeFalse();
        }

        public static SimpleAction ConnectAction(this Window viewWindow,string serviceName) => (SimpleAction) viewWindow.Actions($"Connect{serviceName}").First();
        public static SimpleAction DisconnectAction(this Window viewWindow,string serviceName) => (SimpleAction) viewWindow.Actions($"Disconnect{serviceName}").First();

        public static void Actions_are_Activated_For_CurrentUser_Details(this XafApplication application, string serviceName){
            var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
            compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
            var viewWindow = application.CreateViewWindow();
                
            viewWindow.SetView(compositeView);

            viewWindow.ConnectAction(serviceName).Active[nameof(ActionsService.ActivateInUserDetails)].ShouldBeTrue();
            viewWindow.DisconnectAction(serviceName).Active[nameof(ActionsService.ActivateInUserDetails)].ShouldBeTrue();

        }

        [SuppressMessage("ReSharper", "UnusedParameter.Global")]
        public static async Task NeedsAuthentication_when_AuthenticationStorage_does_not_contain_current_user(this XafApplication application,Func<IObservable<bool>> needsAuthenticationFactory){
            var needsAuthentication = await needsAuthenticationFactory();
        
            needsAuthentication.ShouldBeTrue();            
        }

        public static async Task Not_NeedsAuthentication_when_AuthenticationStorage_current_user_can_authenticate(
                this IObservable<bool> needsAuthenticationFactory){

            var needsAuthentication = await needsAuthenticationFactory;

            needsAuthentication.ShouldBeFalse();
        }

        public static async Task NeedsAuthentication_when_AuthenticationStorage_current_user_cannot_authenticate<TAuth>(this XafApplication application,Func<IObservable<bool>> needsAuthenticationFactory) where TAuth:CloudOfficeBaseObject{
            await application.NewObjectSpace(space => {
                var msAuthentication = space.CreateObject<TAuth>();
                msAuthentication.Oid = (Guid) SecuritySystem.CurrentUserId;
                space.CommitChanges();
                return Unit.Default.ReturnObservable();
            });

            var needsAuthentication = await needsAuthenticationFactory();

            needsAuthentication.ShouldBeTrue();
        }


    }
}
