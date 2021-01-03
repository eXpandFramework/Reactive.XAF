using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using Google.Apis.Auth.OAuth2;
using JetBrains.Annotations;
using NUnit.Framework;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Office.Cloud.Google.BusinessObjects;
using Xpand.XAF.Modules.Office.Cloud.Tests;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;

namespace Xpand.XAF.Modules.Office.Cloud.Google.Tests{
    [UsedImplicitly]
    public class GoogleServiceTests:CloudServiceTests<GoogleAuthentication>{
        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public void Actions_are_Activated_For_CurrentUser_Details(){
            using var application=Application(Platform.Win);
            application.Actions_are_Activated_For_CurrentUser_Details(ServiceName);
        }


        protected override IObservable<bool> NeedsAuthentication(XafApplication application) => application.GoogleNeedsAuthentication();

        protected override XafApplication Application(Platform platform) => GoogleModule(platform).Application;

        protected GoogleModule GoogleModule( Platform platform=Platform.Win,params ModuleBase[] modules){
            var application = NewApplication(platform,  modules);
            application.SetupGoogleSecurity();
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
        protected override void OnConnect_Action_Creates_Connection(Platform platform, XafApplication application) 
            => GoogleService.CustomAcquireTokenInteractively
                .Do(args => NewAuthentication(platform, application))
                .Do(e => e.Instance=Observable.Empty<UserCredential>().FirstOrDefaultAsync()).Test();
    }
}