using System;
using System.Linq;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;

namespace Xpand.XAF.Modules.Reactive.Tests.FaultContextTests {
    public class FaultContextRxOpTest : FaultContextTestBase {

        [Test]
        public void Provider_Is_Resilient() {
            using var application = Platform.Win.NewApplication<ReactiveModule>();
            
            DefaultReactiveModule(application);
            using var testObserver = application.UseProviderObjectSpace(_ => Observable.Throw<R>(new Exception("test"))).Test();
            
            testObserver.ErrorCount.ShouldBe(0);
            BusObserver.ItemCount.ShouldBe(1);
            new[]{nameof(Provider_Is_Resilient),"UseProviderObjectSpace"}.ShouldAllBe(s => BusObserver.Items.First().ToString().Contains(s));
            
        }
        [Test]
        public void UseObject_Is_Resilient() {
            using var application = Platform.Win.NewApplication<ReactiveModule>();
            
            DefaultReactiveModule(application);
            using var objectSpace = application.CreateObjectSpace<R>();
            var r = objectSpace.CreateObject<R>();
            using var testObserver = application.UseObject(r,_ => Observable.Throw<R>(new Exception("test"))).Test();
            
            testObserver.ErrorCount.ShouldBe(0);
            BusObserver.ItemCount.ShouldBe(1);
            new[]{nameof(UseObject_Is_Resilient),"UseObject"}.ShouldAllBe(s => BusObserver.Items.First().ToString().Contains(s));
            
        }

    }
}