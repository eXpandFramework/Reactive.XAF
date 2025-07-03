using System;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Server.Circuits;
using TestApplication.Blazor.Server.Services;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace TestApplication.Blazor.Server;



public class Startup(IConfiguration configuration) {
    private readonly Func<Startup,Func<IBlazorApplicationBuilder,IObjectSpaceProviderServiceBasedBuilder<IBlazorApplicationBuilder>>> _objectSpaceProviderBuilderSelector;

    public Startup(IConfiguration configuration,
        Func<Startup, Func<IBlazorApplicationBuilder,IObjectSpaceProviderServiceBasedBuilder<IBlazorApplicationBuilder>>>
            objectSpaceProviderBuilderSelector) : this(configuration)
        => _objectSpaceProviderBuilderSelector = objectSpaceProviderBuilderSelector;
    public IConfiguration Configuration { get; } = configuration;

    public void ConfigureServices(IServiceCollection services) {
        services.AddSingleton(typeof(Microsoft.AspNetCore.SignalR.HubConnectionHandler<>), typeof(ProxyHubConnectionHandler<>));
        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddHttpContextAccessor();
        services.AddScoped<CircuitHandler, CircuitHandlerProxy>();
        services.AddXaf(Configuration, builder => {
            builder.UseApplication<TestApplicationBlazorApplication>();
            AddModules(builder);
            AddSecuredObjectSpaceProviders(builder);
            AddSecurity(builder);
            Configure(builder);
        });
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => options.LoginPath = "/LoginPage");
    }

    protected virtual void Configure(IBlazorApplicationBuilder builder) {
        
    }

    protected virtual void AddSecurity(IBlazorApplicationBuilder builder) 
        => builder.Security
            .UseIntegratedMode(options => {
                options.Lockout.Enabled = true;
                options.RoleType = typeof(PermissionPolicyRole);
                options.UserType = typeof(ApplicationUser);
                options.UserLoginInfoType = typeof(ApplicationUserLoginInfo);
                options.UseXpoPermissionsCaching();
                options.Events.OnSecurityStrategyCreated += securityStrategy => ((SecurityStrategy)securityStrategy).PermissionsReloadMode = PermissionsReloadMode.NoCache;
            })
            .AddPasswordAuthentication(options => options.IsSupportChangePassword = true);

    public virtual IObjectSpaceProviderServiceBasedBuilder<IBlazorApplicationBuilder> AddSecuredObjectSpaceProviders(IBlazorApplicationBuilder builder) 
        => _objectSpaceProviderBuilderSelector?.Invoke(this)(builder)??builder.ObjectSpaceProviders.AddSecuredXpo((_, options) => Options(options)).AddNonPersistent();

    public virtual IObjectSpaceProviderServiceBasedBuilder<IBlazorApplicationBuilder> AddObjectSpaceProviders(IBlazorApplicationBuilder builder)
        => builder.ObjectSpaceProviders.AddXpo((_, options) => Options(options)).AddNonPersistent();

    private void Options(XPObjectSpaceProviderOptions options) {
        string connectionString = null;
        if(Configuration.GetConnectionString("ConnectionString") != null) {
            connectionString = Configuration.GetConnectionString("ConnectionString");
        }
        ArgumentNullException.ThrowIfNull(connectionString);
        options.ConnectionString = connectionString;
        options.ThreadSafe = true;
        options.UseSharedDataStoreProvider = true;
    }

    protected virtual void AddModules(IBlazorApplicationBuilder builder) {
        builder.Modules
            .AddAuditTrailXpo()
            // .AddCloningXpo()
            .AddConditionalAppearance()
            .AddDashboards(options => options.DashboardDataType = typeof(DevExpress.Persistent.BaseImpl.DashboardData))
            .AddFileAttachments()
            .AddOffice()
            .AddReports(options => {
                options.EnableInplaceReports = true;
                options.ReportDataType = typeof(DevExpress.Persistent.BaseImpl.ReportDataV2);
                options.ReportStoreMode = DevExpress.ExpressApp.ReportsV2.ReportStoreModes.XML;
            })
            .AddScheduler()
            .AddStateMachine(options
                => options.StateMachineStorageType = typeof(DevExpress.ExpressApp.StateMachine.Xpo.XpoStateMachine))
            .AddValidation(options => options.AllowValidationDetailsAccess = false)
            .AddViewVariants()
            .Add<TestApplicationBlazorModule>()
            ;
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
        if(env.IsDevelopment()) {
            app.UseDeveloperExceptionPage();
        }
        else {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. To change this for production scenarios, see: https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        app.UseHttpsRedirection();
        app.UseRequestLocalization();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseXaf();
        app.UseEndpoints(endpoints => {
            endpoints.MapXafEndpoints();
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
            endpoints.MapControllers();
        });
    }
}