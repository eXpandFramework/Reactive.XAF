using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;
using ListView = DevExpress.ExpressApp.ListView;

namespace Xpand.XAF.Modules.Reactive.Tests.FaultContextTests{
    public class FaultContextActionTest:FaultContextTestBase {
        private static ExecutionPipe CreateDynamicExecutionPipe(ExecutionPipe originalPipe, string methodName) {
            var assemblyName = new AssemblyName("DynamicTestActionAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            var typeBuilder = moduleBuilder.DefineType("DynamicType", TypeAttributes.Public);

            // Store the original delegate in a static field on the new type.
            var delegateField = typeBuilder.DefineField("originalPipeDelegate", typeof(ExecutionPipe), FieldAttributes.Public | FieldAttributes.Static);

            // Define the new method with the desired name.
            var methodBuilder = typeBuilder.DefineMethod(methodName, MethodAttributes.Public, typeof(IObservable<ActionBase>), [typeof(IObservable<ActionBase>)]);
            
            var ilGenerator = methodBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldsfld, delegateField); // Load the static delegate field.
            ilGenerator.Emit(OpCodes.Ldarg_1); // Load the 'source' argument.
            ilGenerator.Emit(OpCodes.Callvirt, typeof(ExecutionPipe).GetMethod("Invoke")!); // Invoke the delegate.
            ilGenerator.Emit(OpCodes.Ret); // Return the result.

            var dynamicType = typeBuilder.CreateType();
            dynamicType.GetField(delegateField.Name)!.SetValue(null, originalPipe);

            var instance = Activator.CreateInstance(dynamicType);
            return (ExecutionPipe)Delegate.CreateDelegate(typeof(ExecutionPipe), instance, methodName);
        }

        [Test][Apartment(ApartmentState.STA)]
        [TestCaseSource(typeof(FaultContextActionEventSelectors), nameof(FaultContextActionEventSelectors.ExecutionSelectors))]
        public void Action_Events_Resilience(ExecutionPipe execution, ActionFactory actionFactory,ActionConfig actionConfig,ActionRepeat actionRepeat, string expectedMethodName) {
            // MODIFICATION: Create and use the dynamic execution pipe.
            var dynamicExecution = CreateDynamicExecutionPipe(execution, expectedMethodName);

            using var application = Platform.Win.NewApplication<ReactiveModule>();
            var actionRegistered = application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.RegisterViewAction(TestContext.CurrentContext.Test.FullName,t => actionFactory(t.controller, t.id)));
            
            var observer = dynamicExecution(actionRegistered).Test();
            
            DefaultReactiveModule(application);
            application.StartWinTest(frame => {
                var action = frame.Action(TestContext.CurrentContext.Test.FullName);
                var whenActionIsEnabled = actionRepeat(action).Take(1);
                DoExecute(action);
                return whenActionIsEnabled
                    .Do(_ => DoExecute(action))
                    .SelectMany(whenActionIsEnabled)
                    .ToUnit();
            });
            
            observer.ErrorCount.ShouldBe(0);
            observer.ItemCount.ShouldBe(0);
            BusObserver.ItemCount.ShouldBe(2);
            var fault = BusObserver.Items.First().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.GetLogicalStackTrace().ToList();
            logicalStack.ShouldNotBeEmpty();
            logicalStack.ShouldContain(frame => frame.MemberName == expectedMethodName);

            
        }

        private static void DoExecute(ActionBase action){
            if (action is PopupWindowShowAction popupWindowShowAction) {
                popupWindowShowAction.DoExecute((Window)popupWindowShowAction.Controller.Frame);
            }
            else {
                action.DoTheExecute();    
            }
        }



        [Test][Apartment(ApartmentState.STA)]
        public void Can_Get_Correct_StackTrace_From_Nested_Method() {
            using var application = Platform.Win.NewApplication<ReactiveModule>(handleExceptions:false);
            application.WhenWin().WhenCustomHandleException().Do(t => t.handledEventArgs.Handled=true).Test();
            SubscribeToActionThatThrows_And_Assert_The_StackTrace(application).Test();
            DefaultReactiveModule(application);
            
            application.StartWinTest(frame => frame.Actions(nameof(Can_Get_Correct_StackTrace_From_Nested_Method))
                .Do(a => a.DoTheExecute()).ToNowObservable());

            BusObserver.AwaitDone(1.Seconds()).ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
        
            fault.InnerException.ShouldBeOfType<InvalidOperationException>();
            fault.InnerException.Message.ShouldBe("Deep error from a nested method.");

            var output = fault.ToString();
            var expectedPattern = $@"  --- Invocation Stack ---.*{nameof(SubscribeToActionThatThrows_And_Assert_The_StackTrace)}";
            output.ShouldMatch(expectedPattern, "The stack trace did not contain the calling method.");

        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> SubscribeToActionThatThrows_And_Assert_The_StackTrace(XafApplication application) {
            return application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.RegisterViewSimpleAction(nameof(Can_Get_Correct_StackTrace_From_Nested_Method)))
                .WhenExecuted(e => 100.Milliseconds().Timer()
                    .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Deep error from a nested method.")))
                    
                );
        }

            
        private IObservable<Unit> GetResilientFailingObservable() 
            => Observable.Throw<Unit>(new InvalidOperationException("Deep error"))
                .ChainFaultContext(["InnerContext"]);

        [Test]
        public void Innermost_WithFaultContext_Takes_Precedence() {
            
            using var application = Platform.Win.NewApplication<ReactiveModule>();
            
            var actionExecuted = application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.RegisterViewSimpleAction(nameof(Innermost_WithFaultContext_Takes_Precedence)))
                .SelectMany(action => action.WhenExecuted(_ => GetResilientFailingObservable()) );
            
            using var testObserver = actionExecuted.PublishFaults().Test();
            DefaultReactiveModule(application);
            var window = application.CreateViewWindow();
            var actionBase = window.Action(nameof(Innermost_WithFaultContext_Takes_Precedence));
            window.SetView(application.NewView<ListView>(typeof(R)));
            
            actionBase.DoTheExecute();
            
            testObserver.ErrorCount.ShouldBe(0);
            BusObserver.ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            
            
            var allContexts = fault.AllContexts().ToArray();
            allContexts.ShouldContain("InnerContext");
            allContexts.ShouldContain(nameof(Innermost_WithFaultContext_Takes_Precedence));
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public void Chained_Action_Helpers_Propagate_Correct_Stack_Trace() {
            using var application = Platform.Win.NewApplication<ReactiveModule>(handleExceptions:false);
            application.WhenWin().WhenCustomHandleException().Do(t => t.handledEventArgs.Handled=true).Test();
            
            SubscribeToActionWithChainedHelpers(application).Test();
            
            DefaultReactiveModule(application);
            
            application.StartWinTest(frame => frame.Actions(nameof(Chained_Action_Helpers_Propagate_Correct_Stack_Trace))
                .Do(a => a.DoTheExecute()).ToNowObservable());

            BusObserver.AwaitDone(1.Seconds()).ItemCount.ShouldBe(1);
            var fault = BusObserver.Items.Single().ShouldBeOfType<FaultHubException>();
            var logicalStack = fault.GetLogicalStackTrace().ToList();
            
            logicalStack.ShouldNotBeNull();
            logicalStack.Count.ShouldBeGreaterThanOrEqualTo(2);
            logicalStack.ShouldContain(frame => frame.MemberName == nameof(SubscribeToActionWithChainedHelpers));
            logicalStack.ShouldContain(frame => frame.MemberName == nameof(ActionsService.WhenExecuted));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IObservable<Unit> SubscribeToActionWithChainedHelpers(XafApplication application) {
            return application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.RegisterViewSimpleAction(nameof(Chained_Action_Helpers_Propagate_Correct_Stack_Trace)))
                .WhenExecuted(_ => 100.Milliseconds().Timer().SelectMany(l => Observable.Throw<Unit>(new InvalidOperationException("Action Failure"))))
                ;
        }
    }
}