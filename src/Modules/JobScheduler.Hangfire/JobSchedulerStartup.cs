using System;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire {
    public class UseHangfire : IStartupFilter {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app => {
                app.UseHangfireServer();
                next(app);
                
            };
    }

    public class JobSchedulerStartup : IHostingStartup{
        public void Configure(IWebHostBuilder builder) 
            => builder.ConfigureServices(services => services
                .AddSingleton<ScheduledJobPersistAttribute>()
                .AddHangfire(ConfigureHangfire)
                .AddHangfireServer()
                .AddSingleton<IStartupFilter, UseHangfire>());

        private static void ConfigureHangfire(IServiceProvider provider,IGlobalConfiguration configuration) 
            => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseDefaultTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseActivator(new ServiceJobActivator(provider.GetService<IServiceScopeFactory>()))
                .UseFilter(provider.GetService<ScheduledJobPersistAttribute>())
                .UseFilter(new AutomaticRetryAttribute(){Attempts = 0});
    }
}