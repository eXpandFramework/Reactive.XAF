using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;

namespace Xpand.XAF.Modules.Reactive.Tests.FaultContextTests{
    public class FaultContextActionTest:FaultContextTestBase {
        [Test][Order(400)]
        public void Can_Execute_Again_On_error() {
            using var application = Platform.Win.NewApplication<ReactiveModule>();
            using var testObserver = application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.RegisterViewSimpleAction(nameof(Can_Execute_Again_On_error)))
                .WhenExecuted(_ => Observable.Throw<Unit>(new Exception()))
                .Test();
            DefaultReactiveModule(application);
            var window = application.CreateViewWindow();
            var actionBase = window.Action(nameof(Can_Execute_Again_On_error));
            window.SetView(application.NewView<ListView>(typeof(R)));
            
            actionBase.DoTheExecute();
            actionBase.DoTheExecute();
            
            testObserver.ItemCount.ShouldBe(0);
            testObserver.ErrorCount.ShouldBe(0);
            BusObserver.ItemCount.ShouldBe(2);
        }
            
            
        private IObservable<Unit> GetFailingObservable(SimpleActionExecuteEventArgs e) => Observable.Throw<Unit>(new InvalidOperationException("Deep error from a nested method."));

        [Test][Order(401)]
        public void Can_Get_Correct_StackTrace_From_Nested_Method() {
            using var application = Platform.Win.NewApplication<ReactiveModule>();
            var actionExecuted = application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.RegisterViewSimpleAction(nameof(Can_Get_Correct_StackTrace_From_Nested_Method)))
                .WhenExecuted(GetFailingObservable); 

            using var testObserver = actionExecuted.Test();
            DefaultReactiveModule(application);
            var window = application.CreateViewWindow();
            var actionBase = window.Action(nameof(Can_Get_Correct_StackTrace_From_Nested_Method));
            window.SetView(application.NewView<ListView>(typeof(R)));

            // ACT
            actionBase.DoTheExecute();

            // ASSERT
            testObserver.ErrorCount.ShouldBe(0);
            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();

            // Corrected Assertion: Check the type and message instead of the StackTrace.
            fault.InnerException.ShouldBeOfType<InvalidOperationException>();
            fault.InnerException.Message.ShouldBe("Deep error from a nested method.");
        }

            
        private IObservable<Unit> GetResilientFailingObservable(SimpleActionExecuteEventArgs e) 
            => Observable.Throw<Unit>(new InvalidOperationException("Deep error"))
                .ChainFaultContext(["InnerContext"]);

        [Test]
        public void Innermost_WithFaultContext_Takes_Precedence() {
            using var application = Platform.Win.NewApplication<ReactiveModule>();
            
            var actionExecuted = application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.RegisterViewSimpleAction(nameof(Innermost_WithFaultContext_Takes_Precedence)))
                .SelectMany(action => action.WhenExecuted()
                    .SelectMany(GetResilientFailingObservable));
            
            using var testObserver = actionExecuted.PublishFaults().Test();
            DefaultReactiveModule(application);
            var window = application.CreateViewWindow();
            var actionBase = window.Action(nameof(Innermost_WithFaultContext_Takes_Precedence));
            window.SetView(application.NewView<ListView>(typeof(R)));
            
            actionBase.DoTheExecute();
            
            testObserver.ErrorCount.ShouldBe(0);
            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            
            fault.Context.CustomContext.ShouldContain("InnerContext");
            
            fault.Context.CustomContext.ShouldNotContain(nameof(Innermost_WithFaultContext_Takes_Precedence));
        }
    }
}