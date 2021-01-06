using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using Microsoft.Identity.Client;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.BusinessObjects;
using Xpand.XAF.Modules.Office.Cloud.Tests;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Tests{
	public class MicrosoftServiceTests:CloudServiceTests<MSAuthentication>{

        protected MicrosoftModule MicrosoftModule( Platform platform=Platform.Win,params ModuleBase[] modules){
            var application = NewApplication(platform,  modules);
            application.SetupSecurity(true);
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
            => MicrosoftService.CustomAcquireTokenInteractively
                .Do(args => application.ObjectSpaceProvider.NewAuthentication(platform))
                .Do(e => e.Instance=Observable.Empty<AuthenticationResult>().FirstOrDefaultAsync()).Test();
    }
}