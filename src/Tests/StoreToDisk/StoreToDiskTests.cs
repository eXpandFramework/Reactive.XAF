using System.IO;
using System.Linq;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.FileExtensions;
using Xpand.Extensions.JsonExtensions;
using Xpand.Extensions.Reactive.Transform.System.IO;
using Xpand.Extensions.StringExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.StoreToDisk.Tests.BOModel;
using Xpand.XAF.Modules.StoreToDisk.Tests.Common;

namespace Xpand.XAF.Modules.StoreToDisk.Tests {
    public class StoreToDiskTests:CommonAppTest {
        [Test][Order(0)]
        public void When_Application_ObjectSpace_Commits_New_Objects() {
            var folder = Application.Model.ToReactiveModule<IModelReactiveModulesStoreToDisk>().StoreToDisk.Folder;
            if (Directory.Exists(folder)) {
                Directory.Delete(folder, true);
            }
            var testObserver = new DirectoryInfo(folder!).WhenFileCreated().FirstAsync().Test();
            var objectSpace = Application.CreateObjectSpace();
            var std = objectSpace.CreateObject<STD>();
            std.Name = nameof(When_Application_ObjectSpace_Commits_New_Objects);
            std.Secret = "secret";
            
            objectSpace.CommitChanges();
            testObserver.AwaitDone(Timeout).ItemCount.ShouldBe(1);
            var changedObserver = new FileInfo($"{folder}\\{typeof(STD).StoreToDiskFileName()}").WhenChanged().FirstAsync().Test();
            changedObserver.AwaitDone(Timeout).ItemCount.ShouldBe(1);
            var fileInfo = testObserver.Items.First();
            var token = fileInfo.ReadAllBytes().UnProtect().First().GetString().DeserializeJson();
            var jToken = token.First();
            jToken[nameof(STD.Secret)].ShouldBe(std.Secret);
            jToken[nameof(STD.Name)].ShouldBe(std.Name);
        }
        
        [Test][Order(1)]
        public void When_Application_ObjectSpace_Updates_Objects() {
            WhenUpdatesObjects(Application.CreateObjectSpace(),nameof(When_Application_ObjectSpace_Updates_Objects));
        }
        
        [Test][Order(2)]
        public void When_ObjectSpaceProvider_ObjectSpace_Updates_Objects() {
            WhenUpdatesObjects(Application.ObjectSpaceProvider.CreateObjectSpace(),nameof(When_ObjectSpaceProvider_ObjectSpace_Updates_Objects));
        }
        
        [Test][Order(3)]
        public void When_Updating_ObjectSpace_Updates_Objects() {
            WhenUpdatesObjects(Application.ObjectSpaceProvider.CreateUpdatingObjectSpace(true),nameof(When_Updating_ObjectSpace_Updates_Objects));
        }

        private void WhenUpdatesObjects(IObjectSpace objectSpace, string secret) {
            var folder = Application.Model.ToReactiveModule<IModelReactiveModulesStoreToDisk>().StoreToDisk.Folder;
            var std = objectSpace.GetObjectsQuery<STD>()
                .First(std => std.Name == nameof(When_Application_ObjectSpace_Commits_New_Objects));
            std.Secret = secret;
            objectSpace.CommitChanges();

            var fileInfo = new FileInfo($"{folder}\\{typeof(STD).StoreToDiskFileName()}");
            var changedObserver = fileInfo.WhenChanged().FirstAsync().Test();
            changedObserver.AwaitDone(Timeout).ItemCount.ShouldBe(1);
            var token = fileInfo.ReadAllBytes().UnProtect().First().GetString().DeserializeJson();
            var jToken = token.First();
            jToken[nameof(STD.Secret)].ShouldBe(std.Secret);
            jToken[nameof(STD.Name)].ShouldBe(std.Name);
        }
    }
}