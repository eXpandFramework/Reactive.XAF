using System;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.Blazor;
using Xpand.TestsLib.Blazor;

[assembly: HostingStartup(typeof(Xpand.XAF.Modules.JobScheduler.Hangfire.HangfireStartup))]
 [assembly: HostingStartup(typeof(HostingStartup))]
 [assembly:HostingStartup(typeof(Xpand.XAF.Modules.Blazor.BlazorStartup))]
namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common {
    public class JobSchedulerStartup : XafHostingStartup<JobSchedulerModule> {
        public JobSchedulerStartup(IConfiguration configuration) : base(configuration) { }

        public override void ConfigureServices(IServiceCollection services) {
            base.ConfigureServices(services);
            services.AddSingleton<ISharedXafApplicationProvider, JobSchedulerApplicationProvider>();
        }
    }

    class JobSchedulerApplicationProvider:TestXafApplicationProvider<JobSchedulerModule> {
        protected override BlazorApplication CreateApplication(IXafApplicationFactory applicationFactory) {
            var blazorApplication = base.CreateApplication(applicationFactory);
            blazorApplication.ConfigureModel();
            return blazorApplication;
        }
        
        public JobSchedulerApplicationProvider(IServiceProvider serviceProvider, IValueManagerStorageContainerInitializer containerInitializer) : base(serviceProvider, containerInitializer) { }
    }
}