using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Server.Circuits;
using TestApplication.Blazor.Server.Services;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using Hangfire;
using Hangfire.Server;
using Humanizer;
using Xpand.Extensions.Reactive.Utility;
using Xpand.XAF.Modules.JobScheduler.Hangfire;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

// [assembly: HostingStartup(typeof(HangfireStartup))]
// [assembly: HostingStartup(typeof(HostingStartup))]
// [assembly:HostingStartup(typeof(BlazorStartup))]
namespace TestApplication.Blazor.Server;
[JobProvider]
public class TestJob {
    protected static readonly ISubject<TestJob> JobsSubject=Subject.Synchronize(new Subject<TestJob>());

    public static IObservable<TestJob> Jobs => JobsSubject.AsObservable().Delay(1.Seconds());

    public PerformContext Context { get; protected set; }

    public TestJob() { }
    public IServiceProvider Provider { get; }

    [ActivatorUtilitiesConstructor]
    protected TestJob(IServiceProvider provider) => Provider = provider;

    [AutomaticRetry(Attempts = 0)][JobProvider]
    public void FailMethodNoRetry() => throw new NotImplementedException();

    [AutomaticRetry(Attempts = 1,DelaysInSeconds = new[] {1})][JobProvider]
    public void FailMethodRetry() => throw new NotImplementedException();

    [JobProvider]
    public void Test() => JobsSubject.OnNext(this);

    [JobProvider]
    public void TestJobId(PerformContext context) {
        Context = context;
            
        JobsSubject.OnNext(this);
    }
}

public class XModule:ModuleBase {
    public override void Setup(ApplicationModulesManager moduleManager) {
        base.Setup(moduleManager);
        moduleManager.WhenApplication(xafApplication => xafApplication.WhenProviderCommitted<JobState>().Select(t => t))
            .Subscribe(this);
        moduleManager.WhenGeneratingModelNodes<IModelJobSchedulerSources>().Take(1)
            .Do(sources => sources.AddNode<IModelJobSchedulerSource>().AssemblyName = GetType().Assembly.GetName().Name)
            .Subscribe(this);
    }
}

public class XafApplicationMonitor1(IXafApplicationFactory innerFactory) : IXafApplicationFactory {
    static XafApplicationMonitor1() {
        XafApplicationMonitor1.Application
            .SelectMany(application => application.WhenSetupComplete())
            .SelectMany(application => application.ObjectSpaceProvider.WhenObjectSpaceCreated(true)
                .SelectMany(space => space.WhenCommiting())
                .Select(state => state))
            .Subscribe();
    }
    private static readonly ISubject<BlazorApplication> ApplicationSubject = Subject.Synchronize(new Subject<BlazorApplication>());
    public static IObservable<BlazorApplication> Application => ApplicationSubject.AsObservable();
    public BlazorApplication CreateApplication() => ApplicationSubject.PushNext(innerFactory.CreateApplication());
}


public class Startup {
    private readonly Func<Startup,Func<IBlazorApplicationBuilder,IObjectSpaceProviderServiceBasedBuilder<IBlazorApplicationBuilder>>> _objectSpaceProviderBuilderSelector;

    public Startup(IConfiguration configuration) {
        Configuration = configuration;
        // GlobalConfiguration.Configuration.UseMemoryStorage(new MemoryStorageOptions());
    }
    
    public Startup(IConfiguration configuration,
        Func<Startup, Func<IBlazorApplicationBuilder,IObjectSpaceProviderServiceBasedBuilder<IBlazorApplicationBuilder>>>
            objectSpaceProviderBuilderSelector) : this(configuration)
        => _objectSpaceProviderBuilderSelector = objectSpaceProviderBuilderSelector;
    public IConfiguration Configuration { get; }
    
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
        // services.AddTransient<XafApplicationMonitor1>();
        // services.Decorate<IXafApplicationFactory, XafApplicationMonitor1>();
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
            .AddCloningXpo()
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
            // .Add<JobSchedulerModule>()
            // .Add<XModule>()
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