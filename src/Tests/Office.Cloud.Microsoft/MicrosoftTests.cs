using System;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Authentication.ExtendedProtection;
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
using Xpand.XAF.Modules.Office.Cloud.Tests;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Tests{
	public class MicrosoftTests:BaseTest{
        private const string ServiceName = "Microsoft";
        [Test][XpandTest()]
        public async Task NeedsAuthentication_when_AuthenticationStorage_does_not_contain_current_user([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=MicrosoftModule(platform).Application){
                await application.NeedsAuthentication_when_AuthenticationStorage_does_not_contain_current_user(() => application.MicrosoftNeedsAuthentication());
            }
        }

        [Test][XpandTest()]
        public async Task NeedsAuthentication_when_AuthenticationStorage_current_user_cannot_authenticate([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=MicrosoftModule(platform).Application){
                await application.NeedsAuthentication_when_AuthenticationStorage_current_user_cannot_authenticate<MSAuthentication>(() =>
                    application.MicrosoftNeedsAuthentication());
            }

        }

        [Test][XpandTest()]
        public async Task Not_NeedsAuthentication_when_AuthenticationStorage_current_user_can_authenticate([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=MicrosoftModule(platform).Application){
                application.ObjectSpaceProvider.NewAuthentication(platform);

                await application.MicrosoftNeedsAuthentication().Not_NeedsAuthentication_when_AuthenticationStorage_current_user_can_authenticate();
            }

        }

        [Test][XpandTest()]
        public void Actions_are_Activated_For_CurrentUser_Details([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=MicrosoftModule(platform).Application){
                application.Actions_are_Activated_For_CurrentUser_Details(ServiceName);
            }
        }

        [Test][XpandTest()]
        public  void Actions_Active_State_when_authentication_needed([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=MicrosoftModule(platform).Application){
                application.Actions_Active_State_when_authentication_needed(ServiceName);
            }
        }

        
        [Test][XpandTest()]
        public async Task Actions_Active_State_when_authentication_not_needed([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=MicrosoftModule(platform).Application){
                application.ObjectSpaceProvider.NewAuthentication(platform);
                
                await application.Actions_Active_State_when_authentication_not_needed(ServiceName);
            }
        }


        [Test]
        [XpandTest()]
        public async Task DisconnectMicrosoft_Action_Destroys_Connection([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=MicrosoftModule(platform).Application){
                application.ObjectSpaceProvider.NewAuthentication(platform);
                await application.DisconnectMicrosoft_Action_Destroys_Connection(ServiceName);
            }
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
	                .Do(args => application.ObjectSpaceProvider.NewAuthentication(platform))
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