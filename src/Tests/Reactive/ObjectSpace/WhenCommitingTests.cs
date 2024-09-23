using System.Linq;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.Attributes;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Tests.Common;

namespace Xpand.XAF.Modules.Reactive.Tests.ObjectSpace {
    public class WhenCommitingTests:ReactiveCommonAppTest {
        [TestCase(true)]
        [TestCase(false)]
        public void WhenCommiting_Deleted(bool emitAfterCommitted) {
            var objectSpace = Application.CreateObjectSpace();
            var o1 = objectSpace.CreateObject<R>();
            var o2 = objectSpace.CreateObject<R>();
            objectSpace.CommitChanges();
            var testObserver = objectSpace.WhenCommiting<R>(ObjectModification.Deleted,emitAfterCommitted).Test();
            
            objectSpace.Delete(o1);
            objectSpace.CommitChanges();
            testObserver.ItemCount.ShouldBe(1);
            
            testObserver = objectSpace.WhenCommiting<R>(ObjectModification.Deleted,emitAfterCommitted).Test();
            objectSpace.Delete(o2);
            objectSpace.CommitChanges();
            testObserver.ItemCount.ShouldBe(1);
        }

        [Test]
        public void WhenProviderCommittedNew() {
            var testObserver = Application.WhenProviderCommitted<R>(ObjectModification.New).Test();

            var objectSpace = Application.CreateObjectSpace();
            var r = objectSpace.CreateObject<R>();
            objectSpace.CommitChanges();
            r.Test = "test";
            objectSpace.CommitChanges();
            
            // var o = objectSpace.CreateObject<R2>();
            objectSpace.CommitChanges();
            // o.Test = "test";
            // objectSpace.CommitChanges();
            
            testObserver.ItemCount.ShouldBe(1);
        }
        [Test]
        public void WhenProviderCommittedDetailed() {
            using var testObserver = Application.WhenProviderCommittedDetailed(typeof(R),ObjectModification.NewOrUpdated).ToObjects().Test();

            var objectSpace = Application.ObjectSpaceProvider.CreateObjectSpace();
            var r = objectSpace.CreateObject<R>();
            objectSpace.CommitChanges();
            r.Test = "test";
            objectSpace.CommitChanges();
            
            objectSpace.CreateObject<R2>();
            objectSpace.CommitChanges();

            testObserver.Items.All(o1 => o1 is R).ShouldBeTrue();
            testObserver.ItemCount.ShouldBe(2);
        }
        [Test]
        public void WhenProviderCommittedDetailedNewModified() {
            using var space = Application.CreateObjectSpace();
            space.Delete(space.GetObjectsQuery<R>().ToArray());
            space.CommitChanges();
            var testObserver = Application.WhenProviderObject<R>(ObjectModification.NewOrUpdated,modifiedProperties:
                [nameof(R.Active)],criteriaExpression:arg => arg.Active).Test();

            var objectSpace = Application.ObjectSpaceProvider.CreateObjectSpace();
            var r = objectSpace.CreateObject<R>();
            r.Test = "test";
            objectSpace.CommitChanges();

            
            testObserver.ItemCount.ShouldBe(1);
        }

    }
}