using System;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Base;
using Fasterflect;
using Hangfire;
using Hangfire.Dashboard;
using HarmonyLib;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.Harmony;
using Xpand.Extensions.Reactive.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.SecurityExtensions;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Services;
using StartupExtensions = DevExpress.ExpressApp.Blazor.Services.StartupExtensions;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Hangfire {
    public class UseHangfire : IStartupFilter {
        static UseHangfire() =>
            typeof(StartupExtensions).Method(nameof(StartupExtensions.UseXaf),Flags.StaticPublic)
                .PatchWith(postFix:new HarmonyMethod(typeof(UseHangfire),nameof(UseXaf)));

        public static void UseXaf(IApplicationBuilder builder) => Dashboard?.Invoke(builder);

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) {
            return app => {
                var serviceProvider = app.ApplicationServices;
                JobSchedulerService.JobState.TakeUntil(serviceProvider.WhenApplicationStopping())
                    .Where(state => state.State == WorkerState.Succeeded)
                    .Where(state => state.JobWorker.Job.ChainJobs.Any())
                    .SelectMany(state => state.WhenSucceeded().WhenNeedTrigger().TakeUntil(serviceProvider.WhenApplicationStopping())
                        .SelectMany(_ => state.JobWorker.Job.ChainJobs.ToNowObservable().DoItemResilient(job => job.Job.Trigger(serviceProvider))))
                    .Finally(() => {})
                    .Subscribe();
                next(app);
            };
        }

        public static readonly Action<IApplicationBuilder> Dashboard = builder 
            => builder.UseHangfireDashboard(options:new DashboardOptions {
                Authorization = [new DashboardAuthorization()]
            });
    }

    public class DashboardAuthorization : IDashboardAuthorizationFilter {
        public bool Authorize(DashboardContext context) {
            var httpContext = context.GetHttpContext();
            return httpContext.User.Identity!.IsAuthenticated && httpContext.RequestServices.RunWithStorageAsync(application => application.DeferItemResilient(() => {
                var security = application.Security;
                if (!security.IsSecurityStrategyComplex()) return true.Observe();
                if (security.IsActionPermissionGranted(nameof(JobSchedulerService.JobDashboard))) return true.Observe();
                using var objectSpace = application.CreateObjectSpace(security?.UserType);
                var user = (ISecurityUserWithRoles)objectSpace.FindObject(security?.UserType,
                    CriteriaOperator.Parse($"{nameof(ISecurityUser.UserName)}=?", httpContext.User.Identity.Name));
                return user.Roles.Cast<IPermissionPolicyRole>().Any(role => role.IsAdministrative).Observe();
            })).Result;
        }
    }

    public class HangfireStartup : IHostingStartup{
        public void Configure(IWebHostBuilder builder) 
            => builder.ConfigureServices(services => services
                .AddHangfire(ConfigureHangfire)
                .AddHangfireServer()
                .AddSingleton<IStartupFilter, UseHangfire>()
                .AddSingleton<IHangfireJobFilter>(provider => new HangfireJobFilter(provider))
            );

        private static void ConfigureHangfire(IServiceProvider provider,IGlobalConfiguration configuration) {
            configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseDefaultTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseActivator(new ServiceJobActivator(provider.GetService<IServiceScopeFactory>()))
                .UseFilter(provider.GetService<IHangfireJobFilter>())
                .UseFilter(new AutomaticRetryAttribute() { Attempts = 0 });
            
            GlobalStateHandlers.Handlers.Add(new ChainJobState.Handler());
        }
    }
    


}