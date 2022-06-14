using System;
using System.Diagnostics;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.Blazor.Services;
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
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.AppDomainExtensions;
using Xpand.Extensions.XAF.SecurityExtensions;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Hangfire {
    public class UseHangfire : IStartupFilter {
        static UseHangfire() =>
            typeof(StartupExtensions).Method(nameof(StartupExtensions.UseXaf),Flags.StaticPublic)
                .PatchWith(postFix:new HarmonyMethod(typeof(UseHangfire),nameof(UseXaf)));

        public static void UseXaf(IApplicationBuilder builder) => Dashboard?.Invoke(builder);

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) 
            => app => {
                Server?.Invoke(app);
                next(app);
            };

        public static readonly Action<IApplicationBuilder> Server = builder => builder.UseHangfireServer();
        public static readonly Action<IApplicationBuilder> Dashboard = builder 
            => builder.UseHangfireDashboard(options:new DashboardOptions {
                Authorization = new[] {new DashboardAuthorization()}
        });
    }

    public class DashboardAuthorization : IDashboardAuthorizationFilter {
        public bool Authorize(DashboardContext context) {
            var httpContext = context.GetHttpContext();
            var userIdentity = httpContext.User.Identity;
            Debug.Assert(userIdentity != null, nameof(userIdentity) + " != null");
            if (userIdentity.IsAuthenticated) {
                return httpContext.RequestServices.RunWithStorageAsync(application => {
                    var security = application.Security;
                    if (security.IsSecurityStrategyComplex()) {
                        if (!security.IsActionPermissionGranted(nameof(JobSchedulerService.JobDashboard)) && !security.IsAdminPermissionGranted()) {
                            using var objectSpace = application.CreateObjectSpace(security?.UserType);
                            var user = (ISecurityUserWithRoles)objectSpace.FindObject(security?.UserType,
                                CriteriaOperator.Parse($"{nameof(ISecurityUser.UserName)}=?", userIdentity.Name));
                            return user.Roles.Cast<IPermissionPolicyRole>().Any(role => role.IsAdministrative).ReturnObservable();
                        }
                        return true.ReturnObservable();
                    }
                    return true.ReturnObservable();
                }).Wait(TimeSpan.FromSeconds(10));
                
            }
            return false;
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