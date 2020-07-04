using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
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
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Tests{
	public class MicrosoftTests:BaseTest{
        [Test][XpandTest()]
        public async Task NeedsAuthentication_when_MSAuthentication_does_not_contain_current_user([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=MicrosoftModule(platform).Application){
                
                var needsAuthentication = await application.MicrosoftNeedsAuthentication();

                needsAuthentication.ShouldBeTrue();
            }
        }

        [Test][XpandTest()]
        public async Task NeedsAuthentication_when_MSAuthentication_current_user_cannot_authenticate([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=MicrosoftModule(platform).Application){
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

        [Test][XpandTest()]
        public async Task Not_NeedsAuthentication_when_MSAuthentication_current_user_can_authenticate([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=MicrosoftModule(platform).Application){
                application.ObjectSpaceProvider.NewMicrosoftAuthentication(platform);

                var needsAuthentication = await application.MicrosoftNeedsAuthentication();

                needsAuthentication.ShouldBeFalse();
            }

        }

        [Test][XpandTest()]
        public void Actions_are_Activated_For_CurrentUser_Details([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=MicrosoftModule(platform).Application){

                var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
                compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
                var viewWindow = application.CreateViewWindow();
                
                viewWindow.SetView(compositeView);

                var msActions = viewWindow.Action<MicrosoftModule>();
                msActions.ConnectMicrosoft().Active[nameof(ActionsService.ActivateInUserDetails)].ShouldBeTrue();
                msActions.DisconnectMicrosoft().Active[nameof(ActionsService.ActivateInUserDetails)].ShouldBeTrue();
            }
        }

        [Test][XpandTest()]
        public  void Actions_Active_State_when_authentication_needed([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=MicrosoftModule(platform).Application){
                
                var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
                compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
                var viewWindow = application.CreateViewWindow();
                
                viewWindow.SetView(compositeView);

                var actions = viewWindow.Action<MicrosoftModule>();
                
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
        
        [Test][XpandTest()]
        public async Task Actions_Active_State_when_authentication_not_needed([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=MicrosoftModule(platform).Application){
                application.ObjectSpaceProvider.NewMicrosoftAuthentication(platform);
                var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
                compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
                var viewWindow = application.CreateViewWindow();
                
                viewWindow.SetView(compositeView);

                await ActiveState(viewWindow,false).ConfigureAwait(false);
            }
        }


        [Test]
        [XpandTest()]
        public async Task DisconnectMicrosoft_Action_Destroys_Connection([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=MicrosoftModule(platform).Application){
                application.ObjectSpaceProvider.NewMicrosoftAuthentication(platform);
                var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
                compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
                var viewWindow = application.CreateViewWindow();
                viewWindow.SetView(compositeView);

                var disconnectMicrosoft = viewWindow.Action<MicrosoftModule>().DisconnectMicrosoft();
                await disconnectMicrosoft.WhenActivated().FirstAsync().ToTaskWithoutConfigureAwait();
                disconnectMicrosoft.DoExecute();
                
                disconnectMicrosoft.Active[nameof(MicrosoftService.MicrosoftNeedsAuthentication)].ShouldBeFalse();
                viewWindow.Action<MicrosoftModule>().ConnectMicrosoft().Active[nameof(MicrosoftService.MicrosoftNeedsAuthentication)].ShouldBeTrue();
            }
        }

        public static IEnumerable<Platform> PlatformDatasource(){
            yield return Platform.Web;
            yield return Platform.Win;
        }

        [Test]
        [XpandTest()]
        public async Task ConnectMicrosoft_Action_Creates_Connection([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=MicrosoftModule(platform).Application){
	            
                var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
                compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
                var viewWindow = application.CreateViewWindow();
                viewWindow.SetView(compositeView);
                MicrosoftService.CustomAquireTokenInteractively
	                .Do(args => application.ObjectSpaceProvider.NewMicrosoftAuthentication(platform))
                    .Do(e => e.Instance=Observable.Empty<AuthenticationResult>().FirstOrDefaultAsync()).Test();
                var connectMicrosoft = viewWindow.Action<MicrosoftModule>().ConnectMicrosoft();
                var disconnectMicrosoft = viewWindow.Action<MicrosoftModule>().DisconnectMicrosoft();
                
                connectMicrosoft.DoExecute();

                await connectMicrosoft.WhenDeactivated().FirstAsync().Merge(disconnectMicrosoft.WhenActivated().FirstAsync()).Take(2).ToTaskWithoutConfigureAwait();
                connectMicrosoft.Active[nameof(MicrosoftService.MicrosoftNeedsAuthentication)].ShouldBeFalse();
                disconnectMicrosoft.Active[nameof(MicrosoftService.MicrosoftNeedsAuthentication)].ShouldBeTrue();
            }
        }

        protected MicrosoftModule MicrosoftModule( Platform platform=Platform.Win,params ModuleBase[] modules){
            var application = NewApplication(platform,  modules);
            application.SetupSecurity();
            var module = application.AddModule<MicrosoftModule>();
            application.Model.ConfigureMicrosoft();
            application.Logon();
            application.CreateObjectSpace();
            return module.Application.Modules.OfType<MicrosoftModule>().First();
        }
        XafApplication NewApplication(Platform platform,  ModuleBase[] modules){
            var xafApplication = platform.NewApplication<MicrosoftModule>();
            xafApplication.Modules.AddRange(modules);
            return xafApplication;
        }
    }
}