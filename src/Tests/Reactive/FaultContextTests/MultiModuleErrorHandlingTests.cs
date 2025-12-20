using System;
using System.Reactive;
using System.Reactive.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Relay;
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
        

        [Test][Apartment(ApartmentState.STA)]
        public async Task Error_In_One_Connection_Does_Not_Affect_Other_Modules_And_Is_Handled() {
            await using var application = Platform.Win.NewApplication<ReactiveModule>(handleExceptions:false);
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
            await application.StartWinTest(frame => FaultHub.Bus.Take(4)
                .MergeToUnit(frame.Actions("TestModuleAction").ToNowObservable()
                    .Do(a => a.DoTheExecute()).Do(a => a.DoTheExecute())
                    .MergeToUnit(frame.Actions("RXModuleAction").ToNowObservable()
                        .Do(a => a.DoTheExecute()).Do(a => a.DoTheExecute()))
                    .IgnoreElements().IgnoreElements())
            );
            
            BusEvents.Count.ShouldBe(4);
            appErrorObserver.ItemCount.ShouldBe(4);
        }

    }
}