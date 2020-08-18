using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Office.Cloud.BusinessObjects;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Reactive.Services.Actions;

// ReSharper disable once CheckNamespace
namespace Xpand.XAF.Modules.Office.Cloud.Tests{
    public abstract class CloudServiceTests<TAuthentication>:BaseTest where TAuthentication:CloudOfficeBaseObject{
        protected abstract string ServiceName{ get; }

        protected abstract void NewAuthentication(Platform platform, XafApplication application);

        protected  abstract IObservable<bool> NeedsAuthentication(XafApplication application);

        protected abstract XafApplication Application(Platform platform);
        protected abstract void OnConnectMicrosoft_Action_Creates_Connection(Platform platform, XafApplication application);
        
        [Test][XpandTest()]
        public async Task NeedsAuthentication_when_AuthenticationStorage_does_not_contain_current_user([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=Application(platform)){
                await application.NeedsAuthentication_when_AuthenticationStorage_does_not_contain_current_user(() => NeedsAuthentication(application));
            }
        }
        
        [Test][XpandTest()]
        public async Task NeedsAuthentication_when_AuthenticationStorage_current_user_cannot_authenticate([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=Application(platform)){
                await application.NeedsAuthentication_when_AuthenticationStorage_current_user_cannot_authenticate<TAuthentication>(() =>
                    NeedsAuthentication(application));
            }
        }
        
        [Test][XpandTest()]
        public async Task Not_NeedsAuthentication_when_Authentication_current_user_can_authenticate([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=Application(platform)){
                NewAuthentication(platform, application);
        
                await NeedsAuthentication(application).Not_NeedsAuthentication_when_AuthenticationStorage_current_user_can_authenticate();
            }
        
        }
        
        
        [Test][XpandTest()]
        public void Actions_are_Activated_For_CurrentUser_Details([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=Application(platform)){
        
                application.Actions_are_Activated_For_CurrentUser_Details(ServiceName);
            }
        }
        
        
        
        [Test][XpandTest()]
        public  void Actions_Active_State_when_authentication_needed([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=Application(platform)){
                application.Actions_Active_State_when_authentication_needed(ServiceName);
            }
        }
        
        [Test][XpandTest()]
        public async Task Actions_Active_State_when_authentication_not_needed([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=Application(platform)){
                NewAuthentication(platform, application);
                await application.Actions_Active_State_when_authentication_not_needed(ServiceName);
            }
        }
        
        [Test]
        [XpandTest()]
        public async Task Disconnect_Action_Destroys_Connection([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=Application(platform)){
                NewAuthentication(platform, application);
                await application.Disconnect_Action_Destroys_Connection(ServiceName);
            }
        }
        
        [Test]
        [XpandTest()]
        public async Task ConnectMicrosoft_Action_Creates_Connection([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=Application(platform)){
	            
                var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
                compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
                var viewWindow = application.CreateViewWindow();
                viewWindow.SetView(compositeView);
                OnConnectMicrosoft_Action_Creates_Connection(platform, application);
                var connectMicrosoft = viewWindow.ConnectAction(ServiceName);
                var disconnectMicrosoft = viewWindow.DisconnectAction(ServiceName);
                
                var actionStateChanged = connectMicrosoft.WhenDeactivated().FirstAsync().Merge(disconnectMicrosoft.WhenActivated().FirstAsync()).Take(2).SubscribeReplay();
                connectMicrosoft.DoExecute();

                
                await actionStateChanged.ToTaskWithoutConfigureAwait();
                connectMicrosoft.Active[nameof(Extensions.Office.Cloud.Extensions.NeedsAuthentication)].ShouldBeFalse();
                disconnectMicrosoft.Active[nameof(Extensions.Office.Cloud.Extensions.NeedsAuthentication)].ShouldBeTrue();
            }
        }
        
        
    }
}