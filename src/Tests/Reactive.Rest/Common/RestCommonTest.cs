using System.Linq;
using DevExpress.ExpressApp;
using Microsoft.AspNetCore.Hosting;
using Xpand.Extensions.Blazor;
using Xpand.TestsLib.Blazor;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Rest.Tests.BO;

[assembly: HostingStartup(typeof(HostingStartup))]

namespace Xpand.XAF.Modules.Reactive.Rest.Tests.Common {
    public abstract class RestCommonTest : BlazorCommonTest {
        protected override void ResetXAF() { }

        public RestModule BlazorModule(params ModuleBase[] modules) {
            var newBlazorApplication = NewBlazorApplication(typeof(RestStartup));
            newBlazorApplication.SetupSecurity(userType:typeof(RestUser));
            var module = newBlazorApplication.AddModule<RestModule>(GetType().Assembly.GetTypes().Where(type => type.Namespace==typeof(RestOperationObject).Namespace).ToArray());
            // newBlazorApplication.ConfigureModel();
            newBlazorApplication.Logon();
            using var objectSpace = newBlazorApplication.CreateObjectSpace();
            return module;
        }

    }
}