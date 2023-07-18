using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.StoreToDisk.Tests.BOModel;
using Xpand.XAF.Modules.StoreToDisk.Tests.Common;

namespace Xpand.XAF.Modules.StoreToDisk.Tests {
    public class LoadFromDiskForDefaultValueMembersTests:CommonAppTest {
        [TestCase(true)]
        [TestCase(false)]
        [Order(0)]
        public async Task When_Application_ObjectSpace_Commits_New_Objects(bool protect) {
            await WhenObjectSpaceCommitsNewObjects(nameof(When_Application_ObjectSpace_Commits_New_Objects),Application.CreateObjectSpace(),protect);
        }
        [Test][Order(1)]
        public async Task When_ObjectSpaceProvider_Commits_New_Objects() {
            await WhenObjectSpaceCommitsNewObjects(nameof(When_ObjectSpaceProvider_Commits_New_Objects),Application.ObjectSpaceProvider.CreateObjectSpace());
        }
        
        [Test][Order(2)]
        public async Task When_Updating_ObjectSpace_Commits_New_Objects() {
            await WhenObjectSpaceCommitsNewObjects(nameof(When_Updating_ObjectSpace_Commits_New_Objects),Application.ObjectSpaceProvider.CreateUpdatingObjectSpace(true));
        }

        private async Task WhenObjectSpaceCommitsNewObjects(string name,IObjectSpace objectSpace,bool protect=true) {
            CreateStorage(name, protect);
            objectSpace.CreateObject<STDep>();
            objectSpace.CommitChanges();
            var std = objectSpace.CreateObject<STD>();
            std.Name = name;
            using var testObserver = Application.WhenProviderCommitted<STD>().ToObjects().TakeFirst().Test();

            objectSpace.CommitChanges();

            std = testObserver.AwaitDone(Timeout).Items.Single();
            std.Secret.ShouldBe($"{name}secret");
            std.Number.ShouldBe(2);
            std.Dep.ShouldNotBeNull();
            await Application.UseObjectSpace(space => {
                std = space.GetObject(std);
                space.Delete(std);
                return std.Commit();
            });
            
        }

        
    }
}