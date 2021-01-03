using System;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.Persistent.Base;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.Blazor;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire {
    public class UseHangfire : IStartupFilter {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app => {
                app.UseHangfireServer();
                app.UseHangfireDashboard();
                // app.Use(async (context, next2) => {
                //     if (context.Request.Path.Value.TrimStart('/').StartsWith("api/JobScheduler")) {
                //         app.ApplicationServices.GetService<IValueManagerStorageContainerInitializer>().Initialize();
                //         if(ValueManager.GetValueManager<bool>("ApplicationCreationMarker").Value) {
                //             throw new InvalidOperationException("Application has been already created and cannot be created again in current logical call context.");
                //         }
                //         ValueManager.GetValueManager<bool>("ApplicationCreationMarker").Value = true;
                //         var storage = app.ApplicationServices.GetService<IValueManagerStorageAccessor>().Storage;
                //         var application = app.ApplicationServices.GetService<ISharedXafApplicationProvider>().Application;
                //         var sources = application.Model.ToReactiveModule<IModelReactiveModulesJobScheduler>().JobScheduler.Sources;
                //         return;
                //     }
                //     await next2();
                // });
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