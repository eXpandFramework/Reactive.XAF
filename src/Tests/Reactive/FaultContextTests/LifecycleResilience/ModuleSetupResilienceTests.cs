// using System;
// using System.ComponentModel;
// using System.Linq;
// using System.Reactive;
// using System.Reactive.Linq;
// using System.Reactive.Subjects;
// using System.Threading.Tasks;
// using DevExpress.ExpressApp;
// using DevExpress.ExpressApp.Layout;
// using DevExpress.ExpressApp.Model;
// using DevExpress.ExpressApp.Win;
// using Humanizer;
// using NUnit.Framework;
// using Shouldly;
// using Xpand.Extensions.Reactive.Combine;
// using Xpand.Extensions.Reactive.FaultHub;
// using Xpand.Extensions.Reactive.Transform;
// using Xpand.Extensions.Reactive.Transform.System;
// using Xpand.Extensions.Reactive.Utility;
// using Xpand.XAF.Modules.Reactive.Extensions;
// using Xpand.XAF.Modules.Reactive.Services;
//
// namespace Xpand.XAF.Modules.Reactive.Tests.FaultContextTests.LifecycleResilience{
//     public class ModuleSetupResilienceTests:FaultContextTestBase  {
//         public class MockXafApplication : WinApplication {
//             public MockXafApplication(){
//                 Modules.Add(new ReactiveModule());
//                 SplashScreen = null;
//             }
//
//             [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
//             public int HandleExceptionCount { get; private set; }
//
//             protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args) {
//             }
//
//             protected override void HandleExceptionCore(Exception e) => HandleExceptionCount++;
//             
//             protected override LayoutManager CreateLayoutManagerCore(bool simple) => throw new NotImplementedException();
//         }
//
//
//         private class FailingSetupModule : ModuleBase {
//             public override void Setup(ApplicationModulesManager moduleManager) {
//                 base.Setup(moduleManager);
//                 Observable.Throw<Unit>(new InvalidOperationException("Setup Failed"))
//                     .Subscribe(this);
//             }
//         }
//
//         [Test]
//         public async Task Module_setup_failure_is_isolated_and_reported() {
//             var application = new MockXafApplication();
//             var failingModule = new FailingSetupModule();
//             var setupCompleted = application.WhenSetupComplete().AutoReplayFirst();
//             application.Modules.Add(failingModule);
//             
//             application.Setup();
//             
//             await setupCompleted;
//             application.HandleExceptionCount.ShouldBe(1);
//             BusEvents.Count.ShouldBe(1);
//             var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
//             fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Setup Failed");
//
//             var logicalStack = fault.GetLogicalStackTrace().ToArray();
//             logicalStack.ShouldNotBeEmpty();
//             
//             fault.Context.CustomContext.ShouldNotBeNull();
//             fault.Context.CustomContext.ShouldContain(nameof(FailingSetupModule));
//
//         }
//         
//         private class FailingModuleWithUpstreamContext : ModuleBase {
//             public override void Setup(ApplicationModulesManager moduleManager) {
//                 base.Setup(moduleManager);
//                 Observable.Return(Unit.Default)
//                     .PushStackFrame("UpstreamDirtyFrame")
//                     .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Failure after push")))
//                     .Subscribe(this);
//             }
//         }
//         
//         private class SuccessfulModuleWithUpstreamContext : ModuleBase {
//             public override void Setup(ApplicationModulesManager moduleManager) {
//                 base.Setup(moduleManager);
//                 Observable.Return(Unit.Default)
//                     .PushStackFrame("TransientContext")
//                     .Subscribe(this);
//             }
//         }
//         
//         [Test]
//         public async Task ChainFaultContext_in_Subscribe_resets_the_logical_stack() {
//             var application = new MockXafApplication();
//             var failingModule = new FailingModuleWithUpstreamContext();
//             application.Modules.Add(failingModule);
//             var setupCompleted = application.WhenSetupComplete().AutoReplayFirst();
//             
//             application.Setup();
//             
//             await setupCompleted;
//             BusEvents.Count.ShouldBe(1);
//             var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
//
//             var logicalStack = fault.GetLogicalStackTrace().ToArray();
//             
//             logicalStack.ShouldNotContain(frame => frame.MemberName == "UpstreamDirtyFrame");
//             
//             fault.Context.CustomContext.ShouldContain(nameof(FailingModuleWithUpstreamContext));
//         }
//         
//         [Test]
//         public async Task LogicalStackContext_is_restored_after_successful_subscription() {
//             var application = new MockXafApplication();
//             var successfulModule = new SuccessfulModuleWithUpstreamContext();
//             var setupCompleted = application.WhenSetupComplete().AutoReplayFirst();
//             application.Modules.Add(successfulModule);
//             
//             FaultHub.LogicalStackContext.Value = null;
//             
//             application.Setup();
//
//             await setupCompleted; 
//             FaultHub.LogicalStackContext.Value.ShouldBeNull();
//             BusEvents.ShouldBeEmpty();
//         }
//         
//         private class FailingTypesInfoStreamModule : ModuleBase {
//             public override void Setup(ApplicationModulesManager moduleManager) {
//                 base.Setup(moduleManager);
//                 moduleManager.WhenCustomizeTypesInfo()
//                     .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("TypesInfo customization failed")))
//                     .Subscribe(this);
//             }
//         }
//
//         [Test]
//         public async Task Failure_during_TypesInfo_customization_stream_is_isolated_and_reported() {
//             var application = new MockXafApplication();
//             application.Modules.Add(new FailingTypesInfoStreamModule());
//             var setupCompleted = application.WhenSetupComplete().AutoReplayFirst();
//             
//             application.Setup();
//             
//             application.HandleExceptionCount.ShouldBe(1);
//
//             await setupCompleted;
//             BusEvents.Count.ShouldBe(1);
//             var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
//             fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("TypesInfo customization failed");
//     
//             
//             fault.Context.CustomContext.ShouldContain(nameof(FailingTypesInfoStreamModule));
//         }
//
//         private class FailingModelGeneratorModule : ModuleBase {
//             public override void Setup(ApplicationModulesManager moduleManager) {
//                 base.Setup(moduleManager);
//                 moduleManager.WhenGeneratingModelNodes<IModelViews>()
//                     .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Model generation failed")))
//                     .Subscribe(this);
//             }
//         }
//
//         [Test]
//         public async Task Failure_during_model_generation_stream_is_isolated_and_reported() {
//             var application = new MockXafApplication();
//             application.Modules.Add(new FailingModelGeneratorModule());
//             var setupCompleted = application.WhenSetupComplete().AutoReplayFirst();
//             
//             application.Setup();
//
//             await setupCompleted; 
//             application.HandleExceptionCount.ShouldBe(1);
//
//             BusEvents.Count.ShouldBe(1);
//             var fault = BusEvents.Single().ShouldBeOfType<FaultHubException>();
//             fault.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Model generation failed");
//             
//             fault.Context.CustomContext.ShouldContain(nameof(FailingModelGeneratorModule));
//         }
//
//         private class FailingOneOfMultiStreamsModule : ModuleBase {
//             private readonly Subject<Unit> _subject = new();
//
//             public IObservable<Unit> Bus => _subject.AsObservable();
//
//             public override void Setup(ApplicationModulesManager moduleManager) {
//                 base.Setup(moduleManager);
//                 Observable.Throw<Unit>(new InvalidOperationException(nameof(FailingOneOfMultiStreamsModule)))
//                     .MergeToUnitResilient(Observable.Defer(() => 100.Milliseconds().Timer().Do(_ => _subject.OnNext())))
//                     .Subscribe(this);
//             }
//         }
//
//         [Test]
//         public async Task Errors_Are_Isolated () {
//             
//             var application = new MockXafApplication();
//             var failingModule = new FailingOneOfMultiStreamsModule();
//             application.Modules.Add(failingModule);
//
//
//             var busEmission = failingModule.Bus.Take(1).AutoReplayFirst();
//             var setupCompleted = application.WhenSetupComplete().AutoReplayFirst();
//             
//             
//             application.Setup();
//             await setupCompleted;
//             await busEmission.ToTaskWithoutConfigureAwait();
//             
//             
//             application.HandleExceptionCount.ShouldBe(1);
//             BusEvents.Count.ShouldBe(1);
//             BusEvents.Single().ShouldBeOfType<FaultHubException>()
//                 .InnerException.ShouldBeOfType<InvalidOperationException>()
//                 .Message.ShouldBe(nameof(FailingOneOfMultiStreamsModule));
//         }
//     }
// }
//     
