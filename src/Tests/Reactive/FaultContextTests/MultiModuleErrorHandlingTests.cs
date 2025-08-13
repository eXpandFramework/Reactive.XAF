using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Layout;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using System.ComponentModel;
using System.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp.Win;
using Humanizer;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Reactive.Tests.FaultContextTests {
    [TestFixture]
    public class MultiModuleErrorHandlingTests:FaultContextTestBase {
        
        public class MockXafApplication : WinApplication {

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public int HandleExceptionCount { get; private set; }

            protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args) {
                // base.CreateDefaultObjectSpaceProvider(args);
            }

            protected override void HandleExceptionCore(Exception e) => HandleExceptionCount++;
            
            protected override LayoutManager CreateLayoutManagerCore(bool simple) => throw new NotImplementedException();
        }
        
        public class SuccessfulModule : ModuleBase {
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public bool SetupCompleted { get; private set; }

            public override void Setup(ApplicationModulesManager moduleManager) {
                base.Setup(moduleManager);
                Observable.Return(true)
                    .Do(_ => SetupCompleted = true)
                    .Subscribe(this);
            }
        }
        
        public class FailingModuleWithConnections : ModuleBase {
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public bool SuccessfulConnectionExecuted { get; private set; }
            
            public readonly List<Func<ApplicationModulesManager, IObservable<Unit>>> Connections = [
                _ => Observable.Throw<Unit>(new InvalidOperationException("Connection setup failed")),
                manager => manager.Modules.OfType<FailingModuleWithConnections>()
                    .Do(failingModuleWithConnections
                        => failingModuleWithConnections.SuccessfulConnectionExecuted = true)
                    .ToNowObservable().ToUnit()
            ];

            public override void Setup(ApplicationModulesManager moduleManager) {
                base.Setup(moduleManager);
                Connections.ToNowObservable()
                    .SelectMany(func => func(moduleManager))
                    .Subscribe(this);
            }
        }
        
        [Test]
        public void Error_In_One_Connection_Does_Not_Affect_Other_Modules_And_Is_Handled1() {
            var application = new MockXafApplication();
            var successfulModule = new SuccessfulModule();
            var failingModule = new FailingModuleWithConnections();
            
            application.Modules.Add(failingModule);
            application.Modules.Add(successfulModule);
            
            application.Setup();
            
            application.HandleExceptionCount.ShouldBe(1);
            
            successfulModule.SetupCompleted.ShouldBe(true);
            
            failingModule.SuccessfulConnectionExecuted.ShouldBe(true);
        }

        [Test]
        public void Error_In_One_Connection_Does_Not_Affect_Other_Modules_And_Is_Handled() {
            using var application = Platform.Win.NewApplication<ReactiveModule>(handleExceptions:false);
            application.WhenApplicationModulesManager()
                .Do(manager => {
                    manager.RegisterViewSimpleAction("TestModuleAction")
                        .WhenExecuted(_ => Observable.Throw<Unit>(new Exception()))
                        .Subscribe(manager.Modules.OfType<TestModule>().First());
                    manager.RegisterViewSimpleAction("RXModuleAction")
                        .WhenExecuted(_ => Observable.Throw<Unit>(new Exception()))
                        .Subscribe(manager.Modules.OfType<ReactiveModule>().First());
                })
                .Test();
            var appErrorObserver = application.WhenWin().WhenCustomHandleException().Do(t => t.handledEventArgs.Handled=true).Test();
            DefaultReactiveModule(application);
            application.StartWinTest2(frame => frame.Actions("TestModuleAction").ToNowObservable()
                .Do(a => a.DoTheExecute()).Do(a => a.DoTheExecute())
                .MergeToUnit(frame.Actions("RXModuleAction").ToNowObservable()
                    .Do(a => a.DoTheExecute()).Do(a => a.DoTheExecute()))
            );
            
            BusObserver.ItemCount.ShouldBe(4);
            appErrorObserver.ItemCount.ShouldBe(4);
        }

    }
}