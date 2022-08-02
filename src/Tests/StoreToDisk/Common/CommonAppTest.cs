using System;
using System.Linq;
using DevExpress.ExpressApp.Blazor;
using NUnit.Framework;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.TestsLib.Blazor;
using Xpand.TestsLib.Common;

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

        [OneTimeSetUp]
        public override void Init() {
            base.Init();
            StoreToDiskModule(Application);
        }
    }
}