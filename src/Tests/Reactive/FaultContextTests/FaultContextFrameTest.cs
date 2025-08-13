using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;

namespace Xpand.XAF.Modules.Reactive.Tests.FaultContextTests{
    public class FaultContextFrameTest:FaultContextTestBase {
        [Test]
        public async Task Window_Created_Survives_error() {
            await using var application = Platform.Win.NewApplication<ReactiveModule>(handleExceptions:false);
            using var exceptionSubscription = application.WhenWin().WhenCustomHandleException().Do(t => t.handledEventArgs.Handled=true).Subscribe();
            DefaultReactiveModule(application);
            var captureResult = await application.StartWinTest(_ => 
                    application.WhenWindowCreated(_ => Observable.Throw<Unit>(new Exception()))
                        .TakeUntil(FaultHub.Bus.Take(2).WhenCompleted())
                        .MergeToUnit(application.Observe()
                            .Do(_ => application.CreateViewWindow())
                            .Do(_ => application.CreateViewWindow())
                        ))
                .Capture();
            
            captureResult.Error.ShouldBeNull();
            BusEvents.Count.ShouldBe(2);
        }
        [Test]
        public async Task Frame_Created_Survives_error() {
            await using var application = Platform.Win.NewApplication<ReactiveModule>(handleExceptions:false);
            using var exceptionSubscription = application.WhenWin().WhenCustomHandleException().Do(t => t.handledEventArgs.Handled=true).Subscribe();
            DefaultReactiveModule(application);
            var captureResult = await application.StartWinTest(_ => 
                    application.WhenFrameCreated(_ => Observable.Throw<Unit>(new Exception()))
                        .TakeUntil(FaultHub.Bus.Take(2).WhenCompleted())
                        .MergeToUnit(application.Observe()
                            .Do(_ => application.CreateViewWindow())
                            .Do(_ => application.CreateViewWindow())
                        ))
                .Capture();
            
            captureResult.Error.ShouldBeNull();
            BusEvents.Count.ShouldBe(2);
        }
        
        [Test]
        public async Task WhenFrame_Survives_error() {
            await using var application = Platform.Win.NewApplication<ReactiveModule>(handleExceptions:false);
            using var exceptionSubscription = application.WhenWin().WhenCustomHandleException().Do(t => t.handledEventArgs.Handled=true).Subscribe();
            DefaultReactiveModule(application);
            var captureResult = await application.StartWinTest(_ => 
                    application.WhenFrame(_ => Observable.Throw<Unit>(new Exception()),typeof(R))
                        .TakeUntil(FaultHub.Bus.Take(2).WhenCompleted())
                        .MergeToUnit(application.Observe()
                            .Do(_ => application.CreateViewWindow().SetView(application.NewDetailView(typeof(R))))
                            .Do(_ => application.CreateViewWindow().SetView(application.NewDetailView(typeof(R))))
                        ))
                .Capture();
            
            captureResult.Error.ShouldBeNull();
            BusEvents.Count.ShouldBe(2);
        }
    }
}