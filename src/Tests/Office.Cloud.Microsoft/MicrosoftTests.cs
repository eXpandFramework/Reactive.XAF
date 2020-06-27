using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using Microsoft.Identity.Client;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.BusinessObjects;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Tests{
	public class MicrosoftTests:BaseTest{
        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public async Task NeedsAuthentication_when_MSAuthentication_does_not_contain_current_user(){
            using (var application=CloudModule().Application){
                
                var needsAuthentication = await application.MicrosoftNeedsAuthentication();

                needsAuthentication.ShouldBeTrue();
            }

        }
        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public async Task NeedsAuthentication_when_MSAuthentication_current_user_cannot_authenticate(){
            using (var application=CloudModule().Application){
                await application.NewObjectSpace(space => {
                    var msAuthentication = space.CreateObject<MSAuthentication>();
                    msAuthentication.Oid = (Guid) SecuritySystem.CurrentUserId;
                    space.CommitChanges();
                    return Unit.Default.ReturnObservable();
                });

                var needsAuthentication = await application.MicrosoftNeedsAuthentication();

                needsAuthentication.ShouldBeTrue();
            }

        }
        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public async Task Not_NeedsAuthentication_when_MSAuthentication_current_user_can_authenticate(){
            using (var application=CloudModule().Application){
                application.ObjectSpaceProvider.NewMicrosoftAuthentication();

                var needsAuthentication = await application.MicrosoftNeedsAuthentication();

                needsAuthentication.ShouldBeFalse();
            }

        }

        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public void Cloud_Connection_Actions_are_Activated_For_CurrentUser_Details(){
            using (var application=CloudModule().Application){

                var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
                compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
                var viewWindow = application.CreateViewWindow();
                
                viewWindow.SetView(compositeView);

                var msActions = viewWindow.Action<MicrosoftModule>();
                msActions.ConnectMicrosoft().Active[nameof(ActionsService.ActivateInUserDetails)].ShouldBeTrue();
                msActions.DisconnectMicrosoft().Active[nameof(ActionsService.ActivateInUserDetails)].ShouldBeTrue();
            }
        }
        
        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public async Task CloudConnection_Actions_Active_State_when_authentication_needed(){
            using (var application=CloudModule().Application){
                
                var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
                compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
                var viewWindow = application.CreateViewWindow();
                
                viewWindow.SetView(compositeView);

                var actions = viewWindow.Action<MicrosoftModule>();
                
                await actions.ConnectMicrosoft().WhenActivated().FirstAsync();
                actions.ConnectMicrosoft().Active[nameof(MicrosoftService.MicrosoftNeedsAuthentication)].ShouldBeTrue();
                actions.DisconnectMicrosoft().Active[nameof(MicrosoftService.MicrosoftNeedsAuthentication)].ShouldBeFalse();
            }
        }

        private static async Task ActiveState(Window viewWindow,bool authenticationNeeded){
            var actions = viewWindow.Action<MicrosoftModule>();
            await Observable.Interval(TimeSpan.FromMilliseconds(200))
                .Where(l => actions.ConnectMicrosoft().Active[nameof(MicrosoftService.MicrosoftNeedsAuthentication)]==authenticationNeeded)
                .FirstAsync()
                .ToTaskWithoutConfigureAwait();
            await Observable.Interval(TimeSpan.FromMilliseconds(200))
                .Where(l => actions.DisconnectMicrosoft().Active[nameof(MicrosoftService.MicrosoftNeedsAuthentication)]=!authenticationNeeded)
                .FirstAsync()
                .ToTaskWithoutConfigureAwait();
        }

        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public async Task CloudConnection_Actions_Active_State_when_authentication_not_needed(){
            using (var application=CloudModule().Application){
                application.ObjectSpaceProvider.NewMicrosoftAuthentication();
                var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
                compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
                var viewWindow = application.CreateViewWindow();
                
                viewWindow.SetView(compositeView);

                await ActiveState(viewWindow,false).ConfigureAwait(false);
            }
        }


        [Test]
        [XpandTest()][Apartment(ApartmentState.STA)]
        public async Task DisconnectMicrosoft_Action_Destroys_Connection(){
            using (var application=CloudModule().Application){
                application.ObjectSpaceProvider.NewMicrosoftAuthentication();
                var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
                compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
                var viewWindow = application.CreateViewWindow();
                viewWindow.SetView(compositeView);

                var disconnectMicrosoft = viewWindow.Action<MicrosoftModule>().DisconnectMicrosoft();
                await disconnectMicrosoft.WhenActivated().FirstAsync();
                disconnectMicrosoft.DoExecute();

                await disconnectMicrosoft.WhenDeactivated().FirstAsync();
                disconnectMicrosoft.Active[nameof(MicrosoftService.MicrosoftNeedsAuthentication)].ShouldBeFalse();
                viewWindow.Action<MicrosoftModule>().ConnectMicrosoft().Active[nameof(MicrosoftService.MicrosoftNeedsAuthentication)].ShouldBeTrue();
            }
        }
        [Test]
        [XpandTest()]
        [Apartment(ApartmentState.STA)]
        public async Task ConnectMicrosoft_Action_Creates_Connection(){
            using (var application=CloudModule().Application){
                var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
                compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
                var viewWindow = application.CreateViewWindow();
                viewWindow.SetView(compositeView);
                MicrosoftService.CustomAquireTokenInteractively
                    .Do(e => e.Instance=Observable.Empty<AuthenticationResult>().FirstOrDefaultAsync()).Test();
                var connectMicrosoft = viewWindow.Action<MicrosoftModule>().ConnectMicrosoft();
                await connectMicrosoft.WhenActivated().FirstAsync();
                connectMicrosoft.DoExecute();

                await connectMicrosoft.WhenDeactivated().FirstAsync();
                connectMicrosoft.Active[nameof(MicrosoftService.MicrosoftNeedsAuthentication)].ShouldBeFalse();
                viewWindow.Action<MicrosoftModule>().DisconnectMicrosoft().Active[nameof(MicrosoftService.MicrosoftNeedsAuthentication)].ShouldBeTrue();
            }
        }

        protected MicrosoftModule CloudModule( Platform platform=Platform.Win,params ModuleBase[] modules){
            var application = NewApplication(platform,  modules);
            application.SetupSecurity();
            var module = application.AddModule<MicrosoftModule>(typeof(MSAuthentication));
            
            application.Logon();
            application.CreateObjectSpace();
            return module.Application.Modules.OfType<MicrosoftModule>().First();
        }
        XafApplication NewApplication(Platform platform,  ModuleBase[] modules){
            var xafApplication = platform.NewApplication<ReactiveModule>();
            xafApplication.Modules.AddRange(modules);
            return xafApplication;
        }
    }
}