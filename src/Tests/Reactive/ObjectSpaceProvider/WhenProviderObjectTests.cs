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
    public class WhenProviderObjectTests : ReactiveCommonTest {
        [Test]
        public async Task Emits_Existing_Or_Only_When_Changed_Property() {
            using var application = DefaultReactiveModule().Application;
            await application.UseObjectSpace(space => space.CreateObject<R>().Commit());
            var testObserver = application.WhenProviderObject<R>(ObjectModification.Updated, modifiedProperties: new []{nameof(R.Test)})
                .Select(r => r)
                .Test();
            await application.UseObjectSpace(space => space.GetObjectsQuery<R>().ToArray().Do(r => r.Test1 = "test1").Commit());
            await application.UseObjectSpace(space => space.GetObjectsQuery<R>().ToArray().Do(r => r.Test = "test").Commit());
            
            testObserver.ItemCount.ShouldBe(2);
        }

        [Test]
        public async Task Emits_with_interfaces() {
            using var application = DefaultReactiveModule().Application;
            await application.UseObjectSpace(space => {
                var r = space.CreateObject<R>();
                space.CreateObject<R2>();
                return r.Commit();
            });
            
            var testObserver = application.WhenProviderObject<IR>()
                .Select(r => r)
                .Test();
            
            testObserver.ItemCount.ShouldBe(2);
        }

    }
}