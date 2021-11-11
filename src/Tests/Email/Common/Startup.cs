using System;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using DevExpress.Xpo;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xpand.TestsLib.Blazor;

namespace Xpand.XAF.Modules.Email.Tests.Common {
    public class EmailUser:PermissionPolicyUser {
        public EmailUser(Session session) : base(session) { }
        string _email;
        public override void AfterConstruction() {
            base.AfterConstruction();
            Email = "apostolis.bekiaris@gmail.com";
        }

        public string Email {
            get => _email;
            set => SetPropertyValue(nameof(Email), ref _email, value);
        }
    }
    public class Startup : XafHostingStartup<EmailModule> {
        public Startup(IConfiguration configuration) : base(configuration) { }

        public override void ConfigureServices(IServiceCollection services) {
            base.ConfigureServices(services);
            services.AddSingleton<Xpand.TestsLib.Blazor.XpoDataStoreProviderAccessor>(_ => new XpoDataStoreProviderAccessor());
            services.AddXafSecurity(options => {
                    options.RoleType = typeof(PermissionPolicyRole);
                    options.UserType = typeof(EmailUser);
                    options.Events.OnSecurityStrategyCreated = securityStrategy => ((SecurityStrategy)securityStrategy).RegisterXPOAdapterProviders();
                    options.SupportNavigationPermissionsForTypes = false;
                }).AddExternalAuthentication<HttpContextPrincipalProvider>()
                .AddAuthenticationStandard(options => {
                    options.IsSupportChangePassword = true;
                });
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => {
                options.LoginPath = "/LoginPage";
            });
        }
    }
    
    public class XpoDataStoreProviderAccessor:Xpand.TestsLib.Blazor.XpoDataStoreProviderAccessor{ }
    
    // class ApplicationProvider:TestXafApplicationProvider<EmailModule> {
    //     
    //     protected override BlazorApplication CreateApplication(IXafApplicationFactory applicationFactory) {
    //         var blazorApplication = base.CreateApplication(applicationFactory);
    //         // blazorApplication.ConfigureModel();
    //         return blazorApplication;
    //     }
    //
    //     public ApplicationProvider(IServiceProvider serviceProvider, IValueManagerStorageContainerInitializer containerInitializer) : base(serviceProvider, containerInitializer) { }
    // }
}