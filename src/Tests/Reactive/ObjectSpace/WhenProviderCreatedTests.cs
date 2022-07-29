using System.Linq;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.Attributes;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Tests.Common;

namespace Xpand.XAF.Modules.Reactive.Tests.ObjectSpace {
    public class WhenProviderCreatedTests : ReactiveCommonAppTest {
        
        [Test]
        public void ObjectSpaceCreated() {
            var testObserver = Application.WhenProviderObjectSpaceCreated().Test();

            var objectSpace = Application.ObjectSpaceProvider.CreateObjectSpace();
            
            testObserver.Items.Single().ShouldBe(objectSpace);
        }
        
        [Test]
        public void UpdatingObjectSpaceCreated() {
            var testObserver = Application.WhenProviderObjectSpaceCreated(true).Test();

            var objectSpace = Application.ObjectSpaceProvider.CreateUpdatingObjectSpace(true);

            testObserver.Items.Single().ShouldBe(objectSpace);
        }

        [Test]
        public void WhenProviderCommittedDetailed() {
            var testObserver = Application.WhenProviderCommittedDetailed(typeof(R),ObjectModification.New).Test();

            var objectSpace = Application.ObjectSpaceProvider.CreateObjectSpace();
            var r = objectSpace.CreateObject<R>();
            objectSpace.CreateObject<R2>();
            objectSpace.CommitChanges();
            
            testObserver.Items.SelectMany(t => t.details.Select(t1 => t1.instance)).Single().ShouldBe(r);
        }

    }
}