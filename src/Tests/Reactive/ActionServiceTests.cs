using System.Linq;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;

namespace Xpand.XAF.Modules.Reactive.Tests{
    public class ActionServiceTests:ReactiveCommonTest{
        [TestCase(ViewType.ListView,false)]
        [XpandTest()][TestCase(ViewType.DetailView,true)]
        public void ActivateForCurrentUser(ViewType viewType, bool active) {
            using var application = Platform.Web.NewApplication<ReactiveModule>();
            var testObserver = application.WhenApplicationModulesManager().SelectMany(manager => manager.RegisterViewSimpleAction("test"))
                .ActivateInUserDetails()
                .Test();
            application.SetupSecurity();
            DefaultReactiveModule(application);
            application.Logon();
            var compositeView = application.NewView(viewType, application.Security.UserType);
            compositeView.CurrentObject = compositeView.ObjectSpace.GetObjectByKey(application.Security.UserType,SecuritySystem.CurrentUserId);
                
            application.CreateViewWindow().SetView(compositeView);
                
            testObserver.ItemCount.ShouldBe(active?1:0);
            if (active){
                testObserver.Items.First().Active[nameof(ActionsService.ActivateInUserDetails)].ShouldBeTrue();    
            }
        }
        
        [XpandTest()]
        [Test()]
        public void ActivateWhenEnabled() {
            using var application = Platform.Web.NewApplication<ReactiveModule>();
            var testObserver = application.WhenApplicationModulesManager().SelectMany(manager => manager.RegisterViewSimpleAction("test"))
                .ActivateWhenEnabled()
                .Test();
            DefaultReactiveModule(application);
            application.Logon();
            var compositeView = application.NewView(ViewType.DetailView, typeof(R));
            var window = application.CreateViewWindow();
            window.SetView(compositeView);
            var action = window.Action("test");

            action.Enabled[""] = false;
            
            testObserver.ItemCount.ShouldBe(1);
            
            action.Active[nameof(ActionsService.ActivateWhenEnabled)].ShouldBeFalse();

            action.Enabled[""] = true;

            action.Active[nameof(ActionsService.ActivateWhenEnabled)].ShouldBeTrue();
        }

    }
}