using System;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.Blazor;
using Xpand.TestsLib.Blazor;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests {
    public class JobSchedulerStartup : XafHostingStartup<JobSchedulerModule> {
        public JobSchedulerStartup(IConfiguration configuration) : base(configuration) { }

        public override void ConfigureServices(IServiceCollection services) {
            base.ConfigureServices(services);
            services.AddSingleton<ISharedXafApplicationProvider, JobSchedulerApplicationProvider>();
        }
    }

    class JobSchedulerApplicationProvider:TestXafApplicationProvider<JobSchedulerModule> {
        
        protected override BlazorApplication NewBlazorApplication() {
            var newBlazorApplication = base.NewBlazorApplication();
            newBlazorApplication.ConfigureModel();
            return newBlazorApplication;
        }

        public JobSchedulerApplicationProvider(IServiceProvider serviceProvider, IValueManagerStorageContainerInitializer containerInitializer) : base(serviceProvider, containerInitializer) { }
    }
}