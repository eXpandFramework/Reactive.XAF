using NUnit.Framework;
using Shouldly;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Tests.Common;

namespace Xpand.XAF.Modules.Reactive.Tests.ObjectSpace {
    public class OptimisticLockingTests:ReactiveCommonAppTest {
        [Test]
        public void Modify_Different_properties_With_Different_ObjectSpace() {
            var objectSpace = Application.CreateObjectSpace();
            var r = objectSpace.CreateObject<R>();
            objectSpace.CommitChanges();
            r.Test += nameof(R.Test);
            var objectSpace1 = Application.CreateObjectSpace();
            var o1 = objectSpace1.GetObject(r);
            o1.Test1 = nameof(R.Test1);
        
            objectSpace1.CommitChanges();
            objectSpace.CommitChanges();

            var objectSpace2 = Application.CreateObjectSpace();
            r = objectSpace2.GetObject(r);
            r.Test.ShouldBe(nameof(R.Test));
            r.Test1.ShouldBe(nameof(R.Test1));
        }

    }
}