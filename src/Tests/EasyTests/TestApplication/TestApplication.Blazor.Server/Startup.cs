using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.ApplicationBuilder;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Blazor.SystemModule;
using DevExpress.ExpressApp.Office.Blazor;
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
using Xpand.XAF.Modules.JobScheduler.Hangfire.Hangfire;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using Fasterflect;
using HarmonyLib;
using TestApplication.Module;
using TestApplication.Module.Blazor;
using Xpand.Extensions.Harmony;

[assembly: HostingStartup(typeof(GoogleCodeStateStartup))]
[assembly: HostingStartup(typeof(HostingStartup))]
[assembly: HostingStartup(typeof(HangfireStartup))]
[assembly: HostingStartup(typeof(Xpand.XAF.Modules.Blazor.BlazorStartup))]
namespace TestApplication.Blazor.Server {
    public class Startup {
        public static void CheckValueOnInsert(ModuleList __instance,object value) {
            if (value.GetType() == typeof(SystemBlazorModule)) {
                
            }
            if (__instance.FindModule(value.GetType()) != null)
                throw new ArgumentException(string.Format("The {0} module has already been added.", (object) value.GetType().FullName), nameof (value));   
        }
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services){
            typeof(ModuleList).Method("CheckValueOnInsert").PatchWith(new HarmonyMethod(GetType(),"CheckValueOnInsert"));
         services.AddSingleton(typeof(Microsoft.AspNetCore.SignalR.HubConnectionHandler<>), typeof(ProxyHubConnectionHandler<>));

        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddHttpContextAccessor();
        services.AddScoped<CircuitHandler, CircuitHandlerProxy>();
        services.AddXaf(Configuration, builder => {
            builder.UseApplication<ServerBlazorApplication>();
            builder.Modules
                .AddAuditTrailXpo()
                .AddCloningXpo()
                .AddConditionalAppearance()
                .AddDashboards(options => {
                    options.DashboardDataType = typeof(DevExpress.Persistent.BaseImpl.DashboardData);
                })
                .AddFileAttachments()
                .AddOffice()
                .AddReports(options => {
                    options.EnableInplaceReports = true;
                    options.ReportDataType = typeof(DevExpress.Persistent.BaseImpl.ReportDataV2);
                    options.ReportStoreMode = DevExpress.ExpressApp.ReportsV2.ReportStoreModes.XML;
                })
                .AddStateMachine(options => {
                    options.StateMachineStorageType = typeof(DevExpress.ExpressApp.StateMachine.Xpo.XpoStateMachine);
                })
                .AddValidation(options => {
                    options.AllowValidationDetailsAccess = false;
                })
                .AddViewVariants()
                .Add<TestBlazorModule>()
            	// .Add<DXApplication24BlazorModule>()
                ;
            builder.ObjectSpaceProviders
                .AddSecuredXpo((serviceProvider, options) => {
                    string connectionString = null;
                    if(Configuration.GetConnectionString("ConnectionString") != null) {
                        connectionString = Configuration.GetConnectionString("ConnectionString");
                    }
#if EASYTEST
                    if(Configuration.GetConnectionString("EasyTestConnectionString") != null) {
                        connectionString = Configuration.GetConnectionString("EasyTestConnectionString");
                    }
#endif
                    ArgumentNullException.ThrowIfNull(connectionString);
                    options.ConnectionString = connectionString;
                    options.ThreadSafe = true;
                    options.UseSharedDataStoreProvider = true;
                })
                .AddNonPersistent();
            builder.Security
                .UseIntegratedMode(options => {
                    options.RoleType = typeof(PermissionPolicyRole);
                    // ApplicationUser descends from PermissionPolicyUser and supports the OAuth authentication. For more information, refer to the following topic: https://docs.devexpress.com/eXpressAppFramework/402197
                    // If your application uses PermissionPolicyUser or a custom user type, set the UserType property as follows:
                    options.UserType = typeof(User);
                    // ApplicationUserLoginInfo is only necessary for applications that use the ApplicationUser user type.
                    // If you use PermissionPolicyUser or a custom user type, comment out the following line:
                    // options.UserLoginInfoType = typeof(DXApplication24.Module.BusinessObjects.ApplicationUserLoginInfo);
                    options.UseXpoPermissionsCaching();
                })
                .AddPasswordAuthentication(options => {
                    options.IsSupportChangePassword = true;
                });
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
