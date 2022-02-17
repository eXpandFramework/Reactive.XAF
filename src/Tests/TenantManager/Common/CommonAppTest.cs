using System;
using System.Linq;
using DevExpress.ExpressApp.Blazor;
using NUnit.Framework;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.TestsLib.Blazor;
using Xpand.TestsLib.Common;

namespace Xpand.XAF.Modules.TenantManager.Tests.Common {
    public abstract class CommonAppTest:BlazorCommonAppTest{
        protected override Type StartupType => typeof(Startup);
        
        protected BlazorApplication NewBlazorApplication() => NewBlazorApplication(typeof(Startup));

        protected TenantManagerModule EmailModule(BlazorApplication newBlazorApplication) {
            var module = newBlazorApplication.AddModule<TenantManagerModule>(GetType().CollectExportedTypesFromAssembly().ToArray());
            newBlazorApplication.Logon();
            using var objectSpace = newBlazorApplication.CreateObjectSpace();
            return module;
        }

        [OneTimeSetUp]
        public override void Init() {
            base.Init();
            EmailModule(Application);
        }
    }
}