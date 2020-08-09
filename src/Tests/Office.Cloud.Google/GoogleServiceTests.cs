using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using Google.Apis.Auth.OAuth2;
using NUnit.Framework;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Office.Cloud.Google.BusinessObjects;
using Xpand.XAF.Modules.Office.Cloud.Tests;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;

namespace Xpand.XAF.Modules.Office.Cloud.Google.Tests{
    public class GoogleServiceTests:CloudServiceTests<GoogleAuthentication>{
        [Test][XpandTest()]
        public async Task Actions_Active_State_when_authentication_not_needed([ValueSource(nameof(PlatformDatasource))]Platform platform){
            using (var application=Application(platform)){
                NewAuthentication(platform, application);
                await application.Actions_Active_State_when_authentication_not_needed(ServiceName);
            }
        }

        protected override IObservable<bool> NeedsAuthentication(XafApplication application) => application.GoogleNeedsAuthentication();

        protected override XafApplication Application(Platform platform) => GoogleModule(platform).Application;

        protected GoogleModule GoogleModule( Platform platform=Platform.Win,params ModuleBase[] modules){
            var application = NewApplication(platform,  modules);
            application.SetupSecurity(platform==Platform.Win? Guid.Parse("97a41f78-e44a-429b-a743-e4eadd34b60d"):Guid.Parse("f02b6b5c-25aa-48b1-85c8-73aafc85c47c"));
            var module = application.AddModule<GoogleModule>();
            application.Model.ConfigureGoogle(platform);
            application.Logon();
            application.CreateObjectSpace();
            return module.Application.Modules.OfType<GoogleModule>().First();
        }
        XafApplication NewApplication(Platform platform,  ModuleBase[] modules){
            var xafApplication = platform.NewApplication<GoogleModule>();
            xafApplication.Modules.AddRange(modules);
            return xafApplication;
        }

        protected override void NewAuthentication(Platform platform, XafApplication application) => application.ObjectSpaceProvider.NewAuthentication(platform);

        protected override string ServiceName => "Google";
        protected override void OnConnectMicrosoft_Action_Creates_Connection(Platform platform, XafApplication application) 
            => GoogleService.CustomAquireTokenInteractively
                .Do(args => NewAuthentication(platform, application))
                .Do(e => e.Instance=Observable.Empty<UserCredential>().FirstOrDefaultAsync()).Test();
    }
}