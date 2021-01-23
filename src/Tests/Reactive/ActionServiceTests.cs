using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;

namespace Xpand.XAF.Modules.Reactive.Tests{
    public class ActionServiceTests:ReactiveCommonTest{
        [TestCase(ViewType.ListView,false)]
        [XpandTest(IgnoredXAFMinorVersions = "20.1")]
        [Apartment(ApartmentState.STA)]
        [TestCase(ViewType.DetailView,true)]
        public void ActivateForCurrentUser(ViewType viewType, bool active) {
            using var application = Platform.Win.NewApplication<ReactiveModule>();
            var testObserver = application.WhenApplicationModulesManager().SelectMany(manager => manager.RegisterViewSimpleAction("test"))
                .Select(action => action)
                .ActivateInUserDetails()
                .Test();
            application.SetupSecurity();
            DefaultReactiveModule(application);
            ((IModelApplicationNavigationItems) application.Model).NavigationItems.StartupNavigationItem.View =
                application.Model.BOModel.GetClass(typeof(R)).DefaultListView;
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