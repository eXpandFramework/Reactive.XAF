using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Tests.Common;

namespace Xpand.XAF.Modules.Reactive.Tests {
    public class ActionServiceTests : ReactiveCommonTest {
        [TestCase(ViewType.ListView, false)]
        [XpandTest(IgnoredXAFMinorVersions = "20.1")]
        [Apartment(ApartmentState.STA)]
        [TestCase(ViewType.DetailView,true)]
        public async Task ActivateForCurrentUser(ViewType viewType, bool active) {
            using var application = Platform.Win.NewApplication<ReactiveModule>();
            using var testObserver = application.WhenApplicationModulesManager()
	            .SelectMany(manager => manager.RegisterViewSimpleAction("test"))
	            .ActivateInUserDetails()
	            .Test();
            var user = application.SetupSecurity();
            DefaultReactiveModule(application);
            var modelRootNavigationItems = ((IModelApplicationNavigationItems)application.Model).NavigationItems;
            modelRootNavigationItems.StartupNavigationItem = viewType == ViewType.DetailView
                ? modelRootNavigationItems.Items["Default"].Items["MyDetails"]
                : modelRootNavigationItems.Items["Default"]
                    .Items[application.Model.BOModel.GetClass(typeof(R)).DefaultListView.Id];

            await application.Logon(user).FirstAsync();
            
            testObserver.ItemCount.ShouldBe(active?1:0);
            if (active){
                testObserver.Items.First().Active[nameof(ActionsService.ActivateInUserDetails)].ShouldBeTrue();    
            }
        }

        public class ActionExecuteFinishedTests:ReactiveCommonTest {
            [Test]
            public void Emits_When_ExecuteCompleted_by_default() {
                using var application = Platform.Win.NewApplication<ReactiveModule>();
                using var testObserver = application.WhenApplicationModulesManager()
                    .SelectMany(manager => manager.RegisterViewSimpleAction("test"))
                    .WhenExecuteFinished()
                    .Test();
                DefaultReactiveModule(application);
                var window = application.CreateViewWindow();
                var actionBase = window.Action("test");
                window.SetView(application.NewView<ListView>(typeof(R)));
                
                actionBase.DoTheExecute();
                
                testObserver.ItemCount.ShouldBe(1);
            }
            
            [TestCase(false,true)]
            [TestCase(true,false)]
            public void Do_Not_Emit_ByDefault_When_Custom_Emit(bool parameter,bool data) {
                using var application = Platform.Win.NewApplication<ReactiveModule>();
                using var testObserver = application.WhenApplicationModulesManager()
                    .SelectMany(manager => manager.RegisterViewSimpleAction("test",action => action.CustomizeExecutionFinished(data)))
                    .WhenExecuteFinished(parameter)
                    .Test();
                DefaultReactiveModule(application);
                var window = application.CreateViewWindow();
                var actionBase = window.Action("test");
                window.SetView(application.NewView<ListView>(typeof(R)));
                
                actionBase.DoTheExecute();
                
                testObserver.ItemCount.ShouldBe(0);
            }
            
            [TestCase(false,true)]
            [TestCase(true,false)]
            public void Custom_Emit(bool parameter,bool data) {
                using var application = Platform.Win.NewApplication<ReactiveModule>();
                using var testObserver = application.WhenApplicationModulesManager()
                    .SelectMany(manager => manager.RegisterViewSimpleAction("test",action => action.CustomizeExecutionFinished(data)))
                    .WhenExecuteFinished(parameter)
                    .Test();
                DefaultReactiveModule(application);
                var window = application.CreateViewWindow();
                var actionBase = window.Action("test");
                window.SetView(application.NewView<ListView>(typeof(R)));
                
                actionBase.DoTheExecute();
                actionBase.ExecutionFinished();
                
                testObserver.ItemCount.ShouldBe(1);
            }

        }
        public void When_Action_Execute_Finished() {
            
        }

    }
}