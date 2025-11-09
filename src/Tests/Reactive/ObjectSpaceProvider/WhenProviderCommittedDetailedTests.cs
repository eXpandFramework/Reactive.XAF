using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.Attributes;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Tests.Common;

namespace Xpand.XAF.Modules.Reactive.Tests.ObjectSpaceProvider {
    public class WhenProviderCommittedDetailedTests : ReactiveCommonAppTest {
        [Test]
        public void WhenProviderCommittedDetailed() {
            var testObserver = Application.WhenProviderCommittedDetailed(typeof(R),ObjectModification.New).Test();

            var objectSpace = Application.ObjectSpaceProvider.CreateObjectSpace();
            var r = objectSpace.CreateObject<R>();
            objectSpace.CreateObject<R2>();
            objectSpace.CommitChanges();
            
            testObserver.Items.SelectMany(t => t.details.Select(t1 => t1.instance)).Single().ShouldBe(r);
        }
        [Test]
        public async Task WhenProviderCommitting() {
            var testObserver = Application.WhenProviderCommiting<R>().Test();
            
            await Application.UseProviderObjectSpace(space => {
                var o = space.CreateObject<R>();
                o.CommitChanges();
                return o.Observe();
            });
            
            testObserver.ItemCount.ShouldBe(1);
        }

    }
}