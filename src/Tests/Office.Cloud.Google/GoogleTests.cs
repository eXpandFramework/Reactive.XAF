using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Office.Cloud.Google.BusinessObjects;
using Xpand.XAF.Modules.Office.Cloud.Tests;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;

namespace Xpand.XAF.Modules.Office.Cloud.Google.Tests{
	public class GoogleTests:BaseTest{
        private const string ServiceName = "Google";
        [Test][XpandTest()]
        public async Task NeedsAuthentication_when_AuthenticationStorage_does_not_contain_current_user([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=GoogleModule(platform).Application){
                await application.NeedsAuthentication_when_AuthenticationStorage_does_not_contain_current_user(() => application.GoogleNeedsAuthentication());
            }
        }

        [Test][XpandTest()]
        public async Task NeedsAuthentication_when_AuthenticationStorage_current_user_cannot_authenticate([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=GoogleModule(platform).Application){
                await application.NeedsAuthentication_when_AuthenticationStorage_current_user_cannot_authenticate<GoogleAuthentication>(() =>
                    application.GoogleNeedsAuthentication());
            }

        }

        [Test][XpandTest()]
        public async Task Not_NeedsAuthentication_when_MSAuthentication_current_user_can_authenticate([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=GoogleModule(platform).Application){

                application.ObjectSpaceProvider.NewAuthentication(platform);

                await application.GoogleNeedsAuthentication().Not_NeedsAuthentication_when_AuthenticationStorage_current_user_can_authenticate();
            }

        }

        [Test][XpandTest()]
        public void Actions_are_Activated_For_CurrentUser_Details([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=GoogleModule(platform).Application){

                application.Actions_are_Activated_For_CurrentUser_Details(ServiceName);
            }
        }

        [Test][XpandTest()]
        public  void Actions_Active_State_when_authentication_needed([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=GoogleModule(platform).Application){
                application.Actions_Active_State_when_authentication_needed(ServiceName);
            }
        }

        
        [Test][XpandTest()]
        public async Task Actions_Active_State_when_authentication_not_needed([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=GoogleModule(platform).Application){
                application.ObjectSpaceProvider.NewAuthentication(platform);
                await application.Actions_Active_State_when_authentication_not_needed(ServiceName);
            }
        }


        [Test]
        [XpandTest()]
        public async Task DisconnectMicrosoft_Action_Destroys_Connection([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=GoogleModule(platform).Application){
                application.ObjectSpaceProvider.NewAuthentication(platform);
                await application.DisconnectMicrosoft_Action_Destroys_Connection(ServiceName);
            }
        }


        [Test]
        [XpandTest()]
        public async Task ConnectMicrosoft_Action_Creates_Connection([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=GoogleModule(platform).Application){
	            
                // var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
                // compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
                // var viewWindow = application.CreateViewWindow();
                // viewWindow.SetView(compositeView);
                // MicrosoftService.CustomAquireTokenInteractively
	               //  .Do(args => application.ObjectSpaceProvider.NewMicrosoftAuthentication(platform))
                //     .Do(e => e.Instance=Observable.Empty<AuthenticationResult>().FirstOrDefaultAsync()).Test();
                // var connectMicrosoft = viewWindow.Action<MicrosoftModule>().ConnectMicrosoft();
                // var disconnectMicrosoft = viewWindow.Action<MicrosoftModule>().DisconnectMicrosoft();
                //
                // connectMicrosoft.DoExecute();
                //
                // await connectMicrosoft.WhenDeactivated().FirstAsync().Merge(disconnectMicrosoft.WhenActivated().FirstAsync()).Take(2).ToTaskWithoutConfigureAwait();
                // connectMicrosoft.Active[nameof(MicrosoftService.MicrosoftNeedsAuthentication)].ShouldBeFalse();
                // disconnectMicrosoft.Active[nameof(MicrosoftService.MicrosoftNeedsAuthentication)].ShouldBeTrue();
            }
        }

        protected GoogleModule GoogleModule( Platform platform=Platform.Win,params ModuleBase[] modules){
            var application = NewApplication(platform,  modules);
            application.SetupSecurity();
            var module = application.AddModule<GoogleModule>();
            application.Model.ConfigureGoogle();
            application.Logon();
            application.CreateObjectSpace();
            return module.Application.Modules.OfType<GoogleModule>().First();
        }
        XafApplication NewApplication(Platform platform,  ModuleBase[] modules){
            var xafApplication = platform.NewApplication<GoogleModule>();
            xafApplication.Modules.AddRange(modules);
            return xafApplication;
        }
    }
}