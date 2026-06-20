using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.ApplicationBuilder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

[assembly: HostingStartup(typeof(Xpand.Extensions.Blazor.HostingStartup))]
[assembly:HostingStartup(typeof(Xpand.XAF.Modules.Blazor.BlazorStartup))]
namespace Xpand.XAF.Modules.BulkObjectUpdate.Tests.Common {
    class TestModule:ModuleBase {
        
    }
    public class TestStartup(IConfiguration configuration) : TestApplication.Blazor.Server.Startup(configuration) {
        
        protected override void AddModules(IBlazorApplicationBuilder builder) {
            base.AddModules(builder);
            builder.Modules.Add<BulkObjectUpdateModule>();
            builder.Modules.Add<TestModule>();
        }

    }
}