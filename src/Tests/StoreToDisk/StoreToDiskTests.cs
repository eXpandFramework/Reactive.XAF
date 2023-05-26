using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.FileExtensions;
using Xpand.Extensions.JsonExtensions;
using Xpand.Extensions.Reactive.Transform.System.IO;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
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
                CreateStorage(nameof(When_Application_ObjectSpace_Commits_New_Objects));
            }
            var testObserver = new DirectoryInfo(folder!).WhenFileCreated().FirstAsync().Test();
            var objectSpace = Application.CreateObjectSpace();
            var std = objectSpace.CreateObject<STD>();
            std.Name = nameof(When_Application_ObjectSpace_Commits_New_Objects);
            std.Secret = "secret";
            std.Dep = objectSpace.CreateObject<STDep>();
            std.Dep.Name = nameof(STDep);
            std = objectSpace.CreateObject<STD>();
            std.Name = nameof(When_Application_ObjectSpace_Commits_New_Objects);
            std.Secret = "secret1";
            objectSpace.CommitChanges();
            testObserver.AwaitDone(Timeout).ItemCount.ShouldBe(1);
        
            var fileInfo = testObserver.Items.First();
            var byteArray = fileInfo.ReadAllBytes().UnProtect();
            var deserializeJson = byteArray.DeserializeJsonNode().AsArray();
            deserializeJson.Count.ShouldBe(2);
            var jToken = deserializeJson.First();
            jToken[nameof(STD.Secret)].Change<string>().ShouldBe("secret");
            jToken[nameof(STD.Name)].Change<string>().ShouldBe(nameof(When_Application_ObjectSpace_Commits_New_Objects));
            jToken.AsObject().ContainsKey(nameof(STD.Dep)).ShouldBeFalse();
            jToken = deserializeJson.Last();
            jToken[nameof(STD.Secret)].Change<string>().ShouldBe("secret1");
            jToken[nameof(STD.Name)].Change<string>().ShouldBe(nameof(When_Application_ObjectSpace_Commits_New_Objects));
            
            jToken.AsObject().ContainsKey(nameof(STD.Dep)).ShouldBeFalse();
            jToken.AsObject().ContainsKey(nameof(STD.DefaultValue)).ShouldBeFalse();
            jToken.AsObject().ContainsKey(nameof(STD.NotStored)).ShouldBeFalse();
            
            
        }
        
        [Test][Order(1)]
        public void When_Application_ObjectSpace_Updates_Objects() {
            WhenUpdatesObjects(Application.CreateObjectSpace(),nameof(When_Application_ObjectSpace_Updates_Objects));
            WhenUpdatesObjects(Application.CreateObjectSpace(),"2ndTime");
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
            var std = objectSpace.EnsureObject<STD>(std => std.Name == nameof(When_Application_ObjectSpace_Commits_New_Objects),std1 => std1.Name = nameof(When_Application_ObjectSpace_Commits_New_Objects));
            std.Secret = secret;
            objectSpace.CommitChanges();

            var fileInfo = new FileInfo($"{folder}\\{typeof(STD).StoreToDiskFileName()}");
            var jsonArray = fileInfo.ReadAllBytes().UnProtect().DeserializeJsonNode().AsArray();
            jsonArray.Count.ShouldBe(2);
            var jToken = jsonArray.First();
            jToken[nameof(STD.Secret)].Deserialize<string>().ShouldBe(secret);
            jToken[nameof(STD.Name)].Deserialize<string>().ShouldBe(std.Name);
        }
    }
}