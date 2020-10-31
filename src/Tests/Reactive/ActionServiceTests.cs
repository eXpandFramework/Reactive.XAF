using System.Linq;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Reactive.Tests{
    public class ActionServiceTests:ReactiveBaseTest{
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

    }
}