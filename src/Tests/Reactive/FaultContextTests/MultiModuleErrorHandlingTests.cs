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
using DevExpress.ExpressApp.Win;
using Xpand.Extensions.ObjectExtensions;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Tests.FaultContextTests {
    [TestFixture]
    public class MultiModuleErrorHandlingTests {
        
        public class MockXafApplication : WinApplication {

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public int HandleExceptionCount { get; private set; }

            protected override void HandleExceptionCore(Exception e) => HandleExceptionCount++;

            public void SetupModules() {
                
                foreach (var module in Modules) {
                    module.Setup(new ApplicationModulesManager(){Modules = Modules});
                }
            }

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
        public void Error_In_One_Connection_Does_Not_Affect_Other_Modules_And_Is_Handled() {
            var application = new MockXafApplication();
            var successfulModule = new SuccessfulModule();
            var failingModule = new FailingModuleWithConnections();
            
            application.Modules.Add(failingModule);
            application.Modules.Add(successfulModule);
            
            application.SetupModules();
            
            application.HandleExceptionCount.ShouldBe(1);
            
            successfulModule.SetupCompleted.ShouldBe(true);
            
            failingModule.SuccessfulConnectionExecuted.ShouldBe(true);
        }
    }
}