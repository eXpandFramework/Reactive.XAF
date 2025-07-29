using System.Linq;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.Common;

namespace Xpand.XAF.Modules.Reactive.Tests.ObjectSpaceProvider {
    public class WhenProviderCreatedTests : ReactiveCommonAppTest {
        
        [Test]
        public void ObjectSpaceCreated() {
            var testObserver = Application.WhenProviderObjectSpaceCreated().Test();

            var objectSpace = Application.ObjectSpaceProvider.CreateObjectSpace();
            
            testObserver.Items.Single().ShouldBe(objectSpace);
        }
        [Test]
        public void Application_ObjectSpaceCreated() {
            var testObserver = Application.WhenProviderObjectSpaceCreated().Test();

            var objectSpace = Application.CreateObjectSpace();
            
            testObserver.Items.Single().ShouldBe(objectSpace);
        }
        
        [Test]
        public void UpdatingObjectSpaceCreated() {
            var testObserver = Application.WhenProviderObjectSpaceCreated(true).Test();

            var objectSpace = Application.ObjectSpaceProvider.CreateUpdatingObjectSpace(true);

            testObserver.Items.Single().ShouldBe(objectSpace);
        }


    }
}