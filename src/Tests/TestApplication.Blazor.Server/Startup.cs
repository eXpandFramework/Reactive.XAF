using System.Diagnostics.CodeAnalysis;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Server.Circuits;
using TestApplication.Blazor.Server.Services;

namespace TestApplication.Blazor.Server;

public class Startup(IConfiguration configuration)
{
    private readonly Func<Startup, Func<IBlazorApplicationBuilder, IObjectSpaceProviderServiceBasedBuilder<IBlazorApplicationBuilder>>> _objectSpaceProviderBuilderSelector;

    protected Startup(IConfiguration configuration,
        Func<Startup, Func<IBlazorApplicationBuilder, IObjectSpaceProviderServiceBasedBuilder<IBlazorApplicationBuilder>>>
            objectSpaceProviderBuilderSelector) : this(configuration)
        => _objectSpaceProviderBuilderSelector = objectSpaceProviderBuilderSelector;

    public IConfiguration Configuration { get; } = configuration;
    private readonly string _authCookieName = $".AspNetCore.Auth.{Guid.NewGuid():N}";
    protected virtual void ConfigureCookieAuthenticationOptions(CookieAuthenticationOptions options) {
        
        options.Cookie.Name = _authCookieName;
        options.Cookie.MaxAge = TimeSpan.FromSeconds(1);
        options.SlidingExpiration = false;
    }
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(typeof(Microsoft.AspNetCore.SignalR.HubConnectionHandler<>), typeof(ProxyHubConnectionHandler<>));
        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddHttpContextAccessor();
        services.AddScoped<CircuitHandler, CircuitHandlerProxy>();
        
        services.AddXaf(Configuration, builder =>
        {
            builder.UseApplication<TestApplicationBlazorApplication>();
            AddModules(builder);
            AddSecuredObjectSpaceProviders(builder);
            AddSecurity(builder);
            Configure(builder);
        });

        var authentication = services.AddAuthentication(options => {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        });
        authentication.AddCookie(options => {
            options.LoginPath = "/LoginPage";
            ConfigureCookieAuthenticationOptions(options);
        });
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    protected virtual void Configure(IBlazorApplicationBuilder builder) { }

    protected virtual void AddSecurity(IBlazorApplicationBuilder builder) 
        => builder.Security
            .UseIntegratedMode(options => {
                options.Lockout.Enabled = true;
                options.RoleType = typeof(PermissionPolicyRole);
                options.UserType = typeof(BusinessObjects.ApplicationUser);
                options.UserLoginInfoType = typeof(BusinessObjects.ApplicationUserLoginInfo);
                options.UseXpoPermissionsCaching();
                options.Events.OnSecurityStrategyCreated += securityStrategy => {
                    ((SecurityStrategy)securityStrategy).PermissionsReloadMode = PermissionsReloadMode.NoCache;
                };
            })
            .AddPasswordAuthentication(options => {
                options.IsSupportChangePassword = true;
            });

    

    public virtual IObjectSpaceProviderServiceBasedBuilder<IBlazorApplicationBuilder> AddSecuredObjectSpaceProviders(IBlazorApplicationBuilder builder) 
        => _objectSpaceProviderBuilderSelector?.Invoke(this)(builder) ?? builder.ObjectSpaceProviders.AddSecuredXpo((_, options) => Options(options)).AddNonPersistent();

    public virtual IObjectSpaceProviderServiceBasedBuilder<IBlazorApplicationBuilder> AddObjectSpaceProviders(IBlazorApplicationBuilder builder)
        => builder.ObjectSpaceProviders.AddXpo((_, options) => Options(options)).AddNonPersistent();

    private void Options(XPObjectSpaceProviderOptions options)
    {
        string connectionString = null;
        if (Configuration.GetConnectionString("ConnectionString") != null)
        {
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
    }

    protected virtual void AddModules(IBlazorApplicationBuilder builder) {
        builder.Modules
            .AddConditionalAppearance()
            .AddValidation(options => {
                options.AllowValidationDetailsAccess = false;
            })
            .Add<TestApplicationBlazorModule>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. To change this for production scenarios, see: https://aka.ms/aspnetcore-hsts.
            // app.UseHsts();
        }
        app.UseHttpsRedirection();
        app.UseRequestLocalization();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.UseXaf();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapXafEndpoints();
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
            endpoints.MapControllers();
        });
    }
}