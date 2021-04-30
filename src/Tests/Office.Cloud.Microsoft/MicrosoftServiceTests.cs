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
        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public async Task NeedsAuthentication_when_AuthenticationStorage_current_user_cannot_authenticate(){
            using var application=Application(Platform.Win);
            await application.NeedsAuthentication_when_AuthenticationStorage_current_user_cannot_authenticate<MSAuthentication>(() =>
                NeedsAuthentication(application));
        }

        [Test]
        [XpandTest()][Apartment(ApartmentState.STA)]
        public async Task Connect_Action_Creates_Connection(){
            using var application=Application(Platform.Win);
            var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
            compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
            var viewWindow = application.CreateViewWindow();
            viewWindow.SetView(compositeView);
            OnConnect_Action_Creates_Connection(Platform.Win, application);
            var connectMicrosoft = viewWindow.ConnectAction(ServiceName);
            var disconnectMicrosoft = viewWindow.DisconnectAction(ServiceName);
                
            var actionStateChanged = connectMicrosoft.WhenDeactivated().Select(action => action).FirstAsync()
                .Merge(disconnectMicrosoft.WhenActivated().Select(action => action).FirstAsync()).Take(2).SubscribeReplay();
            connectMicrosoft.DoExecute();

                
            await actionStateChanged;
            connectMicrosoft.Active[nameof(Extensions.Office.Cloud.Extensions.NeedsAuthentication)].ShouldBeFalse();
            disconnectMicrosoft.Active[nameof(Extensions.Office.Cloud.Extensions.NeedsAuthentication)].ShouldBeTrue();
        }

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
                .Do(_ => application.ObjectSpaceProvider.NewAuthentication(platform))
                .Do(e => e.Instance=Observable.Empty<AuthenticationResult>().FirstOrDefaultAsync()).Test();
    }
}