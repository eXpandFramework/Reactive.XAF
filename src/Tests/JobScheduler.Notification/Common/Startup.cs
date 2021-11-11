using System;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.Blazor;
using Xpand.TestsLib.Blazor;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Hangfire;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification;

[assembly: HostingStartup(typeof(HangfireStartup))]
 [assembly: HostingStartup(typeof(HostingStartup))]
 [assembly:HostingStartup(typeof(Xpand.XAF.Modules.Blazor.BlazorStartup))]
 // [assembly:HostingStartup(typeof(JobSchedulerNotificationStartup))]
namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Tests.Common {
    public class Startup : XafHostingStartup<JobSchedulerNotificationModule> {
        public Startup(IConfiguration configuration) : base(configuration) { }

        public override void ConfigureServices(IServiceCollection services) {
            base.ConfigureServices(services);
            // services.AddSingleton<ISharedXafApplicationProvider, ApplicationProvider>();
        }
    }

    // class ApplicationProvider:TestXafApplicationProvider<JobSchedulerNotificationModule> {
    //     // protected override BlazorApplication CreateApplication(IXafApplicationFactory applicationFactory) {
    //     //     return base.CreateApplication(applicationFactory);
    //     // }
    //
    //     // protected override BlazorApplication NewBlazorApplication() {
    //     //     var newBlazorApplication = base.NewBlazorApplication();
    //     //     // newBlazorApplication.ConfigureModel();
    //     //     return newBlazorApplication;
    //     // }
    //
    //     public ApplicationProvider(IServiceProvider serviceProvider, IValueManagerStorageContainerInitializer containerInitializer) : base(serviceProvider, containerInitializer) { }
    // }
}