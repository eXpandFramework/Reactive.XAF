using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Tests.Common;

namespace Xpand.XAF.Modules.Reactive.Tests.ObjectSpaceProvider {
    public class WhenProviderObjectTests : ReactiveCommonAppTest {
        [Test]
        public async Task Emits_Existing_Or_Only_When_Changed_Property() {
            await Application.UseObjectSpace(space => space.CreateObject<R>().Commit());
            var testObserver = Application.WhenProviderObject<R>(ObjectModification.Updated, modifiedProperties: new []{nameof(R.Test)})
                .Select(r => r)
                .Test();
            await Application.UseObjectSpace(space => space.GetObjectsQuery<R>().ToArray().Do(r => r.Test1 = "test1").Commit());
            await Application.UseObjectSpace(space => space.GetObjectsQuery<R>().ToArray().Do(r => r.Test = "test").Commit());
            
            testObserver.ItemCount.ShouldBe(2);
        }

    }
}