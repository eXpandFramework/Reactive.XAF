using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Reactive.Tests.FaultContextTests {
    public class FaultContextActionTest : FaultContextTestBase {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<ActionBase> ConsumerMethodWrapper(ResilienceOperator resilienceOperator,
            IObservable<ActionBase> actionRegistered)
            => resilienceOperator(actionRegistered).PushStackFrame();


        [Test]
        [TestCaseSource(typeof(FaultContextActionEventSelectors),
            nameof(FaultContextActionEventSelectors.ExecutionSelectors))]
        public async Task Action_Events_Are_Resilient(ResilienceOperator resilienceOperator,
            ActionFactory actionFactory, ActionConfig actionConfig, ActionRepeat actionRepeat) {
            await using var application = Platform.Win.NewApplication<ReactiveModule>();
            var actionRegistered = application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.RegisterViewAction(TestContext.CurrentContext.Test.FullName,
                    t => actionFactory(t.controller, t.id)));

            ConsumerMethodWrapper(resilienceOperator, actionRegistered).Test();

            DefaultReactiveModule(application);
            await application.StartWinTest(frame => FaultHub.Bus.Take(1)
                .MergeToUnit(Observable.Defer(() => {
                    var action = frame.Action(TestContext.CurrentContext.Test.FullName);
                    var whenActionIsEnabled = actionRepeat(action).Take(1);
                    DoExecute(action);
                    return whenActionIsEnabled.ToUnit();
                }).IgnoreElements()));

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.First().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.ToList();

            logicalStack.ShouldContain(frame => frame.MemberName == nameof(ConsumerMethodWrapper));
        }

        private static void DoExecute(ActionBase action) {
            if (action is PopupWindowShowAction popupWindowShowAction)
                popupWindowShowAction.DoExecute((Window)popupWindowShowAction.Controller.Frame);
            else
                action.DoTheExecute();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> SubscribeToActionThatThrows_And_Assert_The_StackTrace(XafApplication application)
            => application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.RegisterViewSimpleAction(nameof(Can_Get_Correct_StackTrace_From_Nested_Method)))
                .PushStackFrame()
                .WhenExecuted(_ => 100.Milliseconds().Timer()
                    .SelectMany(_
                        => Observable.Throw<Unit>(new InvalidOperationException("Deep error from a nested method.")))
                    .TakeUntil(application.WhenDisposed())
                )
                .TakeUntil(application.WhenDisposed());

        [Test]
        public async Task Can_Execute_Again_On_error() {
            await using var application = Platform.Win.NewApplication<ReactiveModule>();

            application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.RegisterViewSimpleAction(nameof(Can_Execute_Again_On_error)))
                .WhenExecuted(_ => Observable.Throw<Unit>(new Exception()))
                .Subscribe();

            DefaultReactiveModule(application);

            var result = await application.StartWinTest(frame => FaultHub.Bus.Take(2)
                .MergeToUnit(frame.Actions(nameof(Can_Execute_Again_On_error)).ToNowObservable()
                    .DoItemResilient(a => a.DoTheExecute())
                    .DoItemResilient(a => a.DoTheExecute())
                ).IgnoreElements()).Capture();

            result.Error.ShouldBeNull();
            BusEvents.Count.ShouldBe(2);
        }
        [Test]
        public async Task Can_Get_Correct_StackTrace_From_Nested_Method() {
            await using var application = Platform.Win.NewApplication<ReactiveModule>(handleExceptions: false);
            using var exceptionSubscription = application.WhenWin().WhenCustomHandleException()
                .Do(t => t.handledEventArgs.Handled = true).Subscribe();
            using var actionSubscription =
                SubscribeToActionThatThrows_And_Assert_The_StackTrace(application).Subscribe();

            DefaultReactiveModule(application);

            await application.StartWinTest(frame => FaultHub.Bus.Take(1)
                .MergeToUnit(frame.Actions(nameof(Can_Get_Correct_StackTrace_From_Nested_Method))
                    .Do(a => a.DoTheExecute()).ToNowObservable().IgnoreElements()));

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();

            fault.InnerException.ShouldBeOfType<InvalidOperationException>();
            fault.InnerException.Message.ShouldBe("Deep error from a nested method.");

            var logicalStack = fault.LogicalStackTrace.ToList();
            logicalStack.ShouldNotBeNull();

            logicalStack.ShouldContain(frame
                => frame.MemberName == nameof(SubscribeToActionThatThrows_And_Assert_The_StackTrace));
            logicalStack.SelectMany(frame => frame.Context)
                .ShouldContain(context => (string)context == nameof(ActionBase.Executed));
        }


        [Test]
        public async Task Chained_Action_Helpers_Propagate_Correct_Stack_Trace() {
            await using var application = Platform.Win.NewApplication<ReactiveModule>(handleExceptions: false);
            using var testObserver = application.WhenWin().WhenCustomHandleException()
                .Do(t => t.handledEventArgs.Handled = true).Test();

            using var observer = SubscribeToActionWithChainedHelpers(application).Test();

            DefaultReactiveModule(application);

            await application.StartWinTest(frame => FaultHub.Bus.Take(1)
                .MergeToUnit(frame.Actions(nameof(Chained_Action_Helpers_Propagate_Correct_Stack_Trace))
                    .Do(a => a.DoTheExecute()).ToNowObservable().IgnoreElements()));


            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.ToList();

            logicalStack.ShouldNotBeNull();
            logicalStack.Count.ShouldBeGreaterThanOrEqualTo(2);
            logicalStack.ShouldContain(frame => frame.MemberName == nameof(SubscribeToActionWithChainedHelpers));
            logicalStack.SelectMany(frame => frame.Context).ShouldContain(context => (string)context == nameof(ActionBase.Executed));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> SubscribeToActionWithChainedHelpers(XafApplication application)
            => application.WhenApplicationModulesManager()
                .SelectMany(manager
                    => manager.RegisterViewSimpleAction(nameof(Chained_Action_Helpers_Propagate_Correct_Stack_Trace)))
                .WhenExecuted(_ => 100.Milliseconds().Timer()
                    .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Action Failure")))
                    .TakeUntil(application.WhenDisposed()))
                .PushStackFrame()
                .TakeUntil(application.WhenDisposed());

        [Test]
        public async Task WhenExecuted_Resets_The_Logical_Stack() {
            
            await using var application = Platform.Win.NewApplication<ReactiveModule>(handleExceptions: false);
            using var exceptionSubscription = application.WhenWin().WhenCustomHandleException()
                .Do(t => t.handledEventArgs.Handled = true).Subscribe();
            
            FaultHub.LogicalStackContext.Value = new List<LogicalStackFrame> {
                new("DirtyFrame_From_Startup", "startup.cs", 1)
            };

            var actionRegistered = application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.RegisterViewSimpleAction(nameof(WhenExecuted_Resets_The_Logical_Stack)));
            
            using var actionSubscription = actionRegistered
                .WhenExecuted(_ =>
                    Observable.Throw<Unit>(new InvalidOperationException("Action Failed"))
                        .PushStackFrame("Action_Business_Logic")
                )
                .ChainFaultContext(["ActionExecutionStory"])
                .Subscribe();

            DefaultReactiveModule(application);
            
            await application.StartWinTest(frame =>
                FaultHub.Bus.Take(1)
                    .MergeToUnit(frame.Action(nameof(WhenExecuted_Resets_The_Logical_Stack))
                        .Observe().Do(a => a.DoTheExecute()).IgnoreElements())
            );
            
            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.ToArray();
            
            logicalStack.ShouldNotContain(frame => frame.MemberName == "DirtyFrame_From_Startup",
                "The logical stack was not reset and contains frames from a previous context.");

            
            logicalStack.ShouldContain(frame => frame.MemberName == "Action_Business_Logic");
            logicalStack.SelectMany(frame => frame.Context)
                .ShouldContain(context => (string)context == nameof(ActionBase.Executed));
        }

        [Test]
        public async Task PushStackFrame_Builds_A_Coherent_Logical_StackTrace_For_Nested_Operations() {
            
            await using var application = Platform.Win.NewApplication<ReactiveModule>(handleExceptions: true);
            using var exceptionSubscription = application.WhenWin().WhenCustomHandleException()
                .Do(t => t.handledEventArgs.Handled = true).Subscribe();

            
            using var actionSubscription = application.WhenApplicationModulesManager()
                .SelectMany(manager
                    => manager.RegisterViewSimpleAction(
                        nameof(PushStackFrame_Builds_A_Coherent_Logical_StackTrace_For_Nested_Operations)))
                .WhenExecuted(_ => TopLevel_Operation())
                .Subscribe(application);

            DefaultReactiveModule(application);

            
            await application.StartWinTest(frame =>
                FaultHub.Bus.Take(1).MergeToUnit(
                        frame.Action(nameof(PushStackFrame_Builds_A_Coherent_Logical_StackTrace_For_Nested_Operations))
                            .Observe().Do(a => a.DoTheExecute()).IgnoreElements())
            );

            BusEvents.Count.ShouldBe(1);
            var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.LogicalStackTrace.Select(f => f.MemberName).ToArray();

            logicalStack.ShouldNotBeNull();

            logicalStack.ShouldContain(nameof(TopLevel_Operation));
            logicalStack.ShouldContain(nameof(MidLevel_Helper));
            logicalStack.ShouldContain(nameof(LowLevel_Error_Source));

            var topLevelIndex = Array.IndexOf(logicalStack, nameof(TopLevel_Operation));
            var midLevelIndex = Array.IndexOf(logicalStack, nameof(MidLevel_Helper));
            var lowLevelIndex = Array.IndexOf(logicalStack, nameof(LowLevel_Error_Source));

            lowLevelIndex.ShouldBeLessThan(midLevelIndex,
                "The logical stack is not in the correct order: LowLevel should come before MidLevel.");
            midLevelIndex.ShouldBeLessThan(topLevelIndex,
                "The logical stack is not in the correct order: MidLevel should come before TopLevel.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> TopLevel_Operation()
            => MidLevel_Helper().PushStackFrame()
                .ChainFaultContext();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> MidLevel_Helper()
            => LowLevel_Error_Source().PushStackFrame();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> LowLevel_Error_Source()
            => 100.Milliseconds().Timer().SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Failure at the lowest level."))). PushStackFrame();
        
    }
}