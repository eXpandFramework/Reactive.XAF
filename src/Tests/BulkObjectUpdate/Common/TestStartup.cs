using System;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.ApplicationBuilder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

// [assembly: HostingStartup(typeof(Xpand.XAF.Modules.JobScheduler.Hangfire.Hangfire.HangfireStartup))]
[assembly: HostingStartup(typeof(Xpand.Extensions.Blazor.HostingStartup))]
[assembly:HostingStartup(typeof(Xpand.XAF.Modules.Blazor.BlazorStartup))]
namespace Xpand.XAF.Modules.BulkObjectUpdate.Tests.Common {
    public class TestStartup :TestApplication.Blazor.Server.Startup {
        public TestStartup(IConfiguration configuration, Func<TestApplication.Blazor.Server.Startup, Func<IBlazorApplicationBuilder, IObjectSpaceProviderServiceBasedBuilder<IBlazorApplicationBuilder>>> objectSpaceProviderBuilderSelector) : base(configuration, objectSpaceProviderBuilderSelector){
        }

        public TestStartup(IConfiguration configuration) : base(configuration){
        }

        protected override void AddModules(IBlazorApplicationBuilder builder) {
            base.AddModules(builder);
            builder.Modules.Add<BulkObjectUpdateModule>();
            // builder.Modules.Add<TestJobSchedulerModule>();
        }

    }
}