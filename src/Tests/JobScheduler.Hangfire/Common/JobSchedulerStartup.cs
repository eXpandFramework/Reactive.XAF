using System;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.AmbientContext;
using DevExpress.ExpressApp.Blazor.Services;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.Blazor;
using Xpand.TestsLib.Blazor;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Hangfire;

[assembly: HostingStartup(typeof(HangfireStartup))]
 [assembly: HostingStartup(typeof(HostingStartup))]
 [assembly:HostingStartup(typeof(Xpand.XAF.Modules.Blazor.BlazorStartup))]
namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common {
    public class JobSchedulerStartup : XafHostingStartup<JobSchedulerModule> {
        public JobSchedulerStartup(IConfiguration configuration) : base(configuration) { }

        public override void ConfigureServices(IServiceCollection services) {
            base.ConfigureServices(services);
            services.AddSingleton<IHangfireJobFilter,HangfireJobFilter>();
            services.AddSingleton<IBackgroundProcessingServer, BackgroundProcessingServer>();
        }
    }

    class HangfireJobFilter:Hangfire.HangfireJobFilter {
	    public HangfireJobFilter(IServiceProvider provider) : base(provider) { }

	    protected override void ApplyJobState(ApplyStateContext context, IServiceProvider serviceProvider) 
            => ValueManagerContext.RunIsolated(() => context.ApplyJobState(GetApplication(serviceProvider)));

        private static BlazorApplication GetApplication(IServiceProvider serviceProvider) 
            => serviceProvider.GetRequiredService<IXafApplicationProvider>().GetApplication();

        protected override void ApplyPaused(PerformingContext context, IServiceProvider serviceProvider) 
            => ValueManagerContext.RunIsolated(() => context.ApplyPaused(GetApplication(serviceProvider)));
    }

}