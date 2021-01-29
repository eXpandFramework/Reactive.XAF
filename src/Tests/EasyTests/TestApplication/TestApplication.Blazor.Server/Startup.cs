using System;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Blazor.Services;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestApplication.Blazor.Server.Services;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.Office.Cloud.Google.Blazor;
using Xpand.XAF.Modules.JobScheduler.Hangfire;

[assembly: HostingStartup(typeof(GoogleCodeStateStartup))]
[assembly: HostingStartup(typeof(HostingStartup))]
[assembly: HostingStartup(typeof(Xpand.XAF.Modules.JobScheduler.Hangfire.HangfireStartup))]

namespace TestApplication.Blazor.Server {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services){
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddHttpContextAccessor();
            services.AddSingleton<XpoDataStoreProviderAccessor>();
            services.AddScoped<CircuitHandler, CircuitHandlerProxy>();
            services.AddXaf<ServerBlazorApplication>(Configuration);
            services.AddXafSecurity(options => {
                options.RoleType = typeof(DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyRole);
                options.UserType = typeof(DevExpress.Persistent.BaseImpl.PermissionPolicy.PermissionPolicyUser);
                options.Events.OnSecurityStrategyCreated = securityStrategy => ((SecurityStrategy)securityStrategy).RegisterXPOAdapterProviders();
                options.SupportNavigationPermissionsForTypes = false;
            }).AddExternalAuthentication<HttpContextPrincipalProvider>()
                .AddAuthenticationStandard(options => {
                options.IsSupportChangePassword = true;
            });
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => {
                options.LoginPath = "/LoginPage";
            });

            GlobalConfiguration.Configuration.UseMemoryStorage();
            // GlobalConfiguration.Configuration.UseSqlServerStorage(
            //     Configuration.GetConnectionString("ConnectionString"), new SqlServerStorageOptions {
            //         CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            //         SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            //         QueuePollInterval = TimeSpan.Zero,
            //         UseRecommendedIsolationLevel = true,
            //         DisableGlobalLocks = true
            //     });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if(env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }
            else {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();

            
            app.UseXaf();
            // app.UseHangfireDashboard(options: new DashboardOptions {
            //     Authorization = new[] {new DashboardAuthorization()}
            // });

            app.UseEndpoints(endpoints => {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            
        }
    }

}
