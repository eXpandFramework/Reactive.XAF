using System.Linq;
using DevExpress.ExpressApp;

using DevExpress.ExpressApp.Blazor;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.TestsLib.Blazor;
using Xpand.TestsLib.Common;

namespace Xpand.XAF.Modules.RazorView.Tests.Common {
    public abstract class CommonTest : BlazorCommonTest {
        public RazorViewModule RazorViewModule(params ModuleBase[] modules) {
            var newBlazorApplication = NewBlazorApplication();
            return RazorViewModule(newBlazorApplication);
        }
        
        protected BlazorApplication NewBlazorApplication() => NewBlazorApplication(typeof(Startup));

        protected RazorViewModule RazorViewModule(BlazorApplication newBlazorApplication) {
            var module = newBlazorApplication.AddModule<RazorViewModule>(GetType().CollectExportedTypesFromAssembly().ToArray());
            // newBlazorApplication.ConfigureModel();
            newBlazorApplication.Logon();
            using var objectSpace = newBlazorApplication.CreateObjectSpace();
            return module;
        }



    }
}