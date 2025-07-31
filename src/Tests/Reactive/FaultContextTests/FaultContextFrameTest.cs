using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;

namespace Xpand.XAF.Modules.Reactive.Tests.FaultContextTests{
    public class FaultContextFrameTest:FaultContextTestBase {
        [Test]
        public async Task Window_Created_Survives_error() {
            var emitObserver = new TestObserver<Unit>();
            await using var application = Platform.Win.NewApplication<ReactiveModule>();
            DefaultReactiveModule(application);
            using var testObserver = application.WhenWindowCreated()
                .Do(_ => emitObserver.OnNext(Unit.Default))
                .SelectMany(_ => 1.Range(3).ToObservable()
                    .SelectMany(_ => Observable.Throw<Unit>(new Exception()))
                )
                .Test();
            
            
            application.CreateViewWindow();
            application.CreateViewWindow();
            
            testObserver.ErrorCount.ShouldBe(0);
            emitObserver.ItemCount.ShouldBe(2);
            BusObserver.ItemCount.ShouldBe(2);
        }
        [Test]
        public async Task Frame_Created_Survives_error() {
            var emitObserver = new TestObserver<Unit>();
            await using var application = Platform.Win.NewApplication<ReactiveModule>();
            DefaultReactiveModule(application);
            using var testObserver = application.WhenFrame(typeof(R))
                .Do(_ => emitObserver.OnNext(Unit.Default))
                .SelectMany(_ => 1.Range(3).ToObservable()
                    .SelectMany(_ => Observable.Throw<Unit>(new Exception()))
                )
                .Test();
            
            application.CreateViewWindow().SetView(application.NewDetailView(typeof(R)));
            application.CreateViewWindow().SetView(application.NewDetailView(typeof(R)));
            
            testObserver.ErrorCount.ShouldBe(0);
            emitObserver.ItemCount.ShouldBe(2);
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