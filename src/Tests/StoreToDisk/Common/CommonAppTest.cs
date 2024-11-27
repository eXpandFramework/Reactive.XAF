using System;
using System.IO;
using System.Linq;
using DevExpress.ExpressApp.Blazor;
using NUnit.Framework;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.JsonExtensions;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.TestsLib.Blazor;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.StoreToDisk.Tests.BOModel;

namespace Xpand.XAF.Modules.StoreToDisk.Tests.Common {
    public abstract class CommonAppTest:BlazorCommonAppTest{
        protected override Type StartupType => typeof(Startup);
        
        protected BlazorApplication NewBlazorApplication() => NewBlazorApplication(typeof(Startup));

        protected StoreToDiskModule StoreToDiskModule(BlazorApplication newBlazorApplication) {
            var module = newBlazorApplication.AddModule<StoreToDiskModule>(GetType().CollectExportedTypesFromAssembly().ToArray());
            newBlazorApplication.Logon();
            using var objectSpace = newBlazorApplication.CreateObjectSpace();
            return module;
        }

        protected void CreateStorage(string name, bool protect=false) {
            // Application.Modules.FindModule<StoreToDiskModule>().ClearCache();
            // var folder = Application.Model.ToReactiveModule<IModelReactiveModulesStoreToDisk>().StoreToDisk.Folder;
            // if (Directory.Exists(folder)) {
            //     Directory.Delete(folder, true);
            //     Directory.CreateDirectory(folder);
            //     var data = new[] { new { Secret = $"{name}secret", Name = name,Number=2,Dep=0 } }.Serialize();
            //     var bytes = data.Bytes();
            //     if (protect) {
            //         bytes = data.Protect();
            //     }
            //     // bytes.Save($"{folder}\\{typeof(STD).StoreToDiskFileName()}");
            // }
        }
        [OneTimeSetUp]
        public override void Init() {
            base.Init();
            StoreToDiskModule(Application);
        }
    }
}