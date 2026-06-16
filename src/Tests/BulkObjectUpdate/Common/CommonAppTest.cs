using System;
using System.Linq;
using DevExpress.ExpressApp.Blazor;
using NUnit.Framework;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.Xpo.BaseObjects;
using Xpand.TestsLib.Blazor;
using Xpand.TestsLib.Common;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.BulkObjectUpdate.Tests.Common {
    public abstract class CommonAppTest:BlazorCommonAppTest{
        protected override Type StartupType => typeof(Startup);
        
        protected BlazorApplication NewBlazorApplication() => NewBlazorApplication(typeof(Startup));

        protected BulkObjectUpdateModule BulkObjectUpdateModule(BlazorApplication newBlazorApplication) {
            var types = GetType().CollectExportedTypesFromAssembly()
                .Concat(typeof(XPCustomBaseObject).CollectExportedTypesFromAssembly())
                .Concat(typeof(CustomBaseObject).CollectExportedTypesFromAssembly())
                .ToArray();
            var module = newBlazorApplication.AddModule<BulkObjectUpdateModule>(types);
            newBlazorApplication.Logon();
            using var objectSpace = newBlazorApplication.CreateObjectSpace();
            return module;
        }

        [OneTimeSetUp]
        public override void Init() {
            base.Init();
            BulkObjectUpdateModule(Application);
        }
    }
}