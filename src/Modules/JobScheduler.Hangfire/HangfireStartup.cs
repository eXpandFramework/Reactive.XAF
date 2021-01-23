using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Security;
using Fasterflect;
using Hangfire;
using Hangfire.Dashboard;
using HarmonyLib;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.XAF.AppDomainExtensions;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire {
    public class UseHangfire : IStartupFilter {
        static UseHangfire() {
            AppDomain.CurrentDomain.Patch(harmony => {
                var methodInfo = typeof(StartupExtensions).Method(nameof(StartupExtensions.UseXaf),Flags.StaticPublic);
                harmony.Patch(methodInfo,postfix:new HarmonyMethod(typeof(UseHangfire),nameof(UseXaf)));
            });
        }

        public static void UseXaf(IApplicationBuilder builder) => Dashboard?.Invoke(builder);

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) 
            => app => {
                Server?.Invoke(app);
                next(app);
                
            };

        public static Action<IApplicationBuilder> Server = builder => builder.UseHangfireServer();
        public static Action<IApplicationBuilder> Dashboard = builder 
            => builder.UseHangfireDashboard(options:new DashboardOptions {
                Authorization = new[] {new DashboardAuthorization()}
        });
    }

    public class DashboardAuthorization : IDashboardAuthorizationFilter {
        public bool Authorize(DashboardContext context)
            => context.GetHttpContext().User.Identity.IsAuthenticated;
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