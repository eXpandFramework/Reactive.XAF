using System;
using System.Linq;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using Microsoft.Identity.Client;
using Xpand.TestsLib;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.BusinessObjects;
using Xpand.XAF.Modules.Office.Cloud.Tests;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Tests{
	public class MicrosoftServiceTests:CloudServiceTests<MSAuthentication>{
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

        protected override IObservable<bool> NeedsAuthentication(XafApplication application) => application.MicrosoftNeedsAuthentication();

        protected override XafApplication Application(Platform platform) => MicrosoftModule(platform).Application;

        protected override void NewAuthentication(Platform platform, XafApplication application) => application.ObjectSpaceProvider.NewAuthentication(platform);

        protected override string ServiceName => "Microsoft";

        protected override void OnConnect_Action_Creates_Connection(Platform platform, XafApplication application) 
            => MicrosoftService.CustomAquireTokenInteractively
                .Do(args => application.ObjectSpaceProvider.NewAuthentication(platform))
                .Do(e => e.Instance=Observable.Empty<AuthenticationResult>().FirstOrDefaultAsync()).Test();
    }
}