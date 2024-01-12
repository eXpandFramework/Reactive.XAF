using System;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.ApplicationBuilder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using TestApplication.Blazor.Server;
using Xpand.XAF.Modules.Blazor;
using Xpand.XAF.Modules.Reactive;

[assembly: HostingStartup(typeof(Xpand.XAF.Modules.JobScheduler.Hangfire.Hangfire.HangfireStartup))]
[assembly: HostingStartup(typeof(Xpand.Extensions.Blazor.HostingStartup))]
[assembly:HostingStartup(typeof(Xpand.XAF.Modules.Blazor.BlazorStartup))]
namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.NewDirectory1 {
    public class TestStartup :Startup {
        public TestStartup(IConfiguration configuration) : base(configuration) { }
        public TestStartup(IConfiguration configuration, Func<Startup, Func<IBlazorApplicationBuilder,
                IObjectSpaceProviderServiceBasedBuilder<IBlazorApplicationBuilder>>> objectSpaceProviderBuilderSelector)
            : base(configuration, objectSpaceProviderBuilderSelector) { }

        protected override void AddModules(IBlazorApplicationBuilder builder) {
            base.AddModules(builder);
            builder.Modules.Add<JobSchedulerModule>();
            builder.Modules.Add<TestJobSchedulerModule>();
        }

    }
}