using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Tests.Common;

namespace Xpand.XAF.Modules.Reactive.Tests.ObjectSpace {
    public class WhenCommitingTests:ReactiveCommonAppTest {
        [TestCase(true)]
        [TestCase(false)]
        public void WhenCommiting_Deleted(bool emitAfterCommited) {
            var objectSpace = Application.CreateObjectSpace();
            var o1 = objectSpace.CreateObject<R>();
            var o2 = objectSpace.CreateObject<R>();
            objectSpace.CommitChanges();
            var testObserver = objectSpace.WhenCommiting<R>(ObjectModification.Deleted,emitAfterCommited).Test();
            
            objectSpace.Delete(o1);
            objectSpace.CommitChanges();
            testObserver.ItemCount.ShouldBe(1);
            
            testObserver = objectSpace.WhenCommiting<R>(ObjectModification.Deleted,emitAfterCommited).Test();
            objectSpace.Delete(o2);
            objectSpace.CommitChanges();
            testObserver.ItemCount.ShouldBe(1);
        }

    }
}