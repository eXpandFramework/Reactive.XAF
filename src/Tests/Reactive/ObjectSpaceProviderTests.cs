using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Reactive.Tests{
    public class ObjectSpaceProviderTests:ReactiveBaseTest{
        [Test]
        [XpandTest()]
        public void WhenSchemaUpdating(){
            using var application = DefaultReactiveModule().Application;
            var testObserver = application.ObjectSpaceProvider.WhenSchemaUpdating().Test();

            application.ObjectSpaceProvider.UpdateSchema();

            testObserver.ItemCount.ShouldBe(1);
        }

        [Test]
        [XpandTest()]
        public void WhenSchemaUpdated(){
            using var application = DefaultReactiveModule().Application;
            var testObserver = application.ObjectSpaceProvider.WhenSchemaUpdated().Test();

            application.ObjectSpaceProvider.UpdateSchema();

            testObserver.ItemCount.ShouldBe(1);
        }

    }
}