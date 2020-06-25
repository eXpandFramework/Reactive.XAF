using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Office.Cloud.Microsoft;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.Office.Cloud.Tests{
    public class MicrosoftTests:BaseTest{
        [Test][XpandTest()]
        public void Authenticate_Action_Is_Activated_For_CurrentUser_Details(){
            using (var application=CloudModule().Application){

                var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
                compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
                var viewWindow = application.CreateViewWindow();
                
                viewWindow.SetView(compositeView);
                
                viewWindow.Action<MicrosoftModule>().AuthenticateMS().Active.ResultValue.ShouldBeTrue();
            }
        }
        [Test][XpandTest()]
        public async Task Authenticate_Action_Creates_Connection(){
            using (var application=CloudModule().Application){
                application.ObjectSpaceProvider.NewMicrosoftAuthentication();
                var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
                compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
                var viewWindow = application.CreateViewWindow();
                viewWindow.SetView(compositeView);
                var authenticateMS = viewWindow.Action<MicrosoftModule>().AuthenticateMS();
                var modelMicrosoft = application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft();
                
                authenticateMS.DoExecute();

                await Observable.Interval(TimeSpan.FromMilliseconds(200))
	                .FirstAsync(l => authenticateMS.Model.ImageName != modelMicrosoft.ConnectImageName).ToTaskWithoutConfigureAwait();
                authenticateMS.Model.ImageName.ShouldBe(modelMicrosoft.DisconnectImageName);

            }
        }
        [Test][XpandTest()]
        public async Task Authenticate_Action_Destroys_Connection(){
            using (var application=CloudModule().Application){
                application.ObjectSpaceProvider.NewMicrosoftAuthentication();
                await application.AuthorizeMS().FirstAsync();
                var compositeView = application.NewView(ViewType.DetailView, application.Security.UserType);
                compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType, SecuritySystem.CurrentUserId);
                var viewWindow = application.CreateViewWindow();
                viewWindow.SetView(compositeView);
                var authenticateMS = viewWindow.Action<MicrosoftModule>().AuthenticateMS();
                var modelMicrosoft = application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft();
                
                authenticateMS.DoExecute();

                await Observable.Interval(TimeSpan.FromMilliseconds(200))
	                .FirstAsync(l => authenticateMS.Model.ImageName != modelMicrosoft.DisconnectImageName).ToTaskWithoutConfigureAwait();
                authenticateMS.Model.ImageName.ShouldBe(modelMicrosoft.ConnectImageName);

            }
        }

        protected MicrosoftModule CloudModule( Platform platform=Platform.Win,params ModuleBase[] modules){
            var application = NewApplication(platform,  modules);
            application.SetupSecurity();
            var module = application.AddModule<MicrosoftTestModule>(typeof(MSAuthentication));
            
            application.Logon();
            application.CreateObjectSpace();
            return module.Application.Modules.OfType<MicrosoftTestModule>().First();
        }
        XafApplication NewApplication(Platform platform,  ModuleBase[] modules){
            var xafApplication = platform.NewApplication<ReactiveModule>();
            xafApplication.Modules.AddRange(modules);
            return xafApplication;
        }
    }
}