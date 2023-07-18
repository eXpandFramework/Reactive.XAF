using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Conditional;
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
        [TestCase(ViewType.DetailView,true)][Order(0)]
        public async Task ActivateForCurrentUser(ViewType viewType, bool active) {
            using var application = Platform.Win.NewApplication<ReactiveModule>();
            using var testObserver = application.WhenApplicationModulesManager()
	            .SelectMany(manager => manager.RegisterViewSimpleAction(nameof(ActivateForCurrentUser)))
	            .ActivateInUserDetails()
	            .Test();
            var user = application.SetupSecurity();
            DefaultReactiveModule(application);
            var modelRootNavigationItems = ((IModelApplicationNavigationItems)application.Model).NavigationItems;
            modelRootNavigationItems.StartupNavigationItem = viewType == ViewType.DetailView
                ? modelRootNavigationItems.Items["Default"].Items["MyDetails"]
                : modelRootNavigationItems.Items["Default"]
                    .Items[application.Model.BOModel.GetClass(typeof(R)).DefaultListView.Id];

            await application.LogonUser(user).TakeFirst();
            
            testObserver.ItemCount.ShouldBe(active?1:0);
            if (active){
                testObserver.Items.First().Active[nameof(ActionsService.ActivateInUserDetails)].ShouldBeTrue();    
            }
        }

        public class ActionExecuteFinishedTests:ReactiveCommonTest {
            [TestCase(false,true)]
            [TestCase(true,false)][Order(100)]
            public void Do_Not_Emit_ByDefault_When_Custom_Emit(bool parameter,bool data) {
                using var application = Platform.Win.NewApplication<ReactiveModule>();
                using var testObserver = application.WhenApplicationModulesManager()
                    .SelectMany(manager => manager.RegisterViewSimpleAction(nameof(Do_Not_Emit_ByDefault_When_Custom_Emit),action => action.CustomizeExecutionFinished(data)))
                    .WhenExecuteFinished(parameter)
                    .Test();
                DefaultReactiveModule(application);
                var window = application.CreateViewWindow();
                var actionBase = window.Action(nameof(Do_Not_Emit_ByDefault_When_Custom_Emit));
                window.SetView(application.NewView<ListView>(typeof(R)));
                
                actionBase.DoTheExecute();
                
                testObserver.ItemCount.ShouldBe(0);
            }

            [Test][Order(200)]
            public void Emits_When_ExecuteCompleted_by_default() {
                using var application = Platform.Win.NewApplication<ReactiveModule>();
                using var testObserver = application.WhenApplicationModulesManager()
                    .SelectMany(manager => manager.RegisterViewSimpleAction(nameof(Emits_When_ExecuteCompleted_by_default)))
                    .WhenExecuteFinished()
                    .Test();
                DefaultReactiveModule(application);
                var window = application.CreateViewWindow();
                var actionBase = window.Action(nameof(Emits_When_ExecuteCompleted_by_default));
                window.SetView(application.NewView<ListView>(typeof(R)));
                
                actionBase.DoTheExecute();
                
                testObserver.ItemCount.ShouldBe(1);
            }
            
            
            [TestCase(false,true)]
            [TestCase(true,false)][Order(300)]
            public void Custom_Emit(bool parameter,bool data) {
                using var application = Platform.Win.NewApplication<ReactiveModule>();
                using var testObserver = application.WhenApplicationModulesManager()
                    .SelectMany(manager => manager.RegisterViewSimpleAction(nameof(Custom_Emit),action => action.CustomizeExecutionFinished(data)))
                    .WhenExecuteFinished(parameter)
                    .Test();
                DefaultReactiveModule(application);
                var window = application.CreateViewWindow();
                var actionBase = window.Action(nameof(Custom_Emit));
                window.SetView(application.NewView<ListView>(typeof(R)));
                
                actionBase.DoTheExecute();
                actionBase.ExecutionFinished();
                
                testObserver.ItemCount.ShouldBe(1);
            }

        }

    }
}