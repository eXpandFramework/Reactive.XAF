using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Tests.Common;

namespace Xpand.XAF.Modules.Reactive.Tests.ActionTests{
        public class ActionExecuteTests:ReactiveCommonTest {

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