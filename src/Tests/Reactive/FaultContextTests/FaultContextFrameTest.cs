using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;

namespace Xpand.XAF.Modules.Reactive.Tests.FaultContextTests{
    public class FaultContextFrameTest:FaultContextTestBase {
        [Test]
        public async Task Window_Created_Survives_error() {
            var emitObserver = new TestObserver<Unit>();
            await using var application = Platform.Win.NewApplication<ReactiveModule>();
            DefaultReactiveModule(application);
            using var testObserver = application.WhenWindowCreated(_ => {
                    emitObserver.OnNext(Unit.Default);
                    return 1.Range(3).ToObservable()
                        .SelectMany(_ => Observable.Throw<Unit>(new Exception()));
                })
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
            using var testObserver = application.WhenFrameCreated(_ => {
                    emitObserver.OnNext(Unit.Default);
                    return 1.Range(3).ToObservable()
                        .SelectMany(_ => Observable.Throw<Unit>(new Exception()));
                })
                .Test();
            
            application.CreateViewWindow();
            application.CreateViewWindow();
            
            testObserver.ErrorCount.ShouldBe(0);
            emitObserver.ItemCount.ShouldBe(2);
            BusObserver.ItemCount.ShouldBe(2);
        }
        [Test]
        public async Task WhenFrame_Survives_error() {
            var emitObserver = new TestObserver<Unit>();
            await using var application = Platform.Win.NewApplication<ReactiveModule>();
            DefaultReactiveModule(application);
            using var testObserver = application.WhenFrame(_ => {
                    emitObserver.OnNext(Unit.Default);
                    return 1.Range(3).ToObservable()
                        .SelectMany(_ => Observable.Throw<Unit>(new Exception()));
                },typeof(R))
                .Test();
            
            application.CreateViewWindow().SetView(application.NewDetailView(typeof(R)));
            application.CreateViewWindow().SetView(application.NewDetailView(typeof(R)));
            
            testObserver.ErrorCount.ShouldBe(0);
            emitObserver.ItemCount.ShouldBe(2);
            BusObserver.ItemCount.ShouldBe(2);
        }
        
    }
}