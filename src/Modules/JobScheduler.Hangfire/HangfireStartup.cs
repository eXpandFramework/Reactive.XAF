using System;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire {
    public class UseHangfire : IStartupFilter {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) 
            => app => {
                app.UseHangfireServer();
                app.UseHangfireDashboard();
                next(app);
            };
    }

    public class HangfireStartup : IHostingStartup{
        public void Configure(IWebHostBuilder builder) 
            => builder.ConfigureServices(services => services
                .AddHangfire(ConfigureHangfire)
                .AddHangfireServer()
                .AddSingleton<IStartupFilter, UseHangfire>()
                .AddSingleton<HangfireJobFilter>()
            );

        private static void ConfigureHangfire(IServiceProvider provider,IGlobalConfiguration configuration) 
            => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseDefaultTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseActivator(new ServiceJobActivator(provider.GetService<IServiceScopeFactory>()))
                .UseFilter(provider.GetService<HangfireJobFilter>())
                .UseFilter(new AutomaticRetryAttribute(){Attempts = 0});
    }
}