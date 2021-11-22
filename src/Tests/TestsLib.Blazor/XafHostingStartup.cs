using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.AmbientContext;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;

[assembly: HostingStartup(typeof(HostingStartup))]

namespace Xpand.TestsLib.Blazor {
	public abstract class XafHostingStartup<TModule> where TModule : ModuleBase, new() {
		public IConfiguration Configuration { get; }

		protected XafHostingStartup(IConfiguration configuration) => Configuration = configuration;

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
			}
			else {
				app.UseExceptionHandler("/Error");
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();
			app.UseRouting();
			app.UseAuthentication();
			app.UseXaf();
			app.UseEndpoints(endpoints => {
				endpoints.MapBlazorHub();
				endpoints.MapFallbackToPage("/_Host");
			});
		}

		public virtual void ConfigureServices(IServiceCollection services) {
			services.AddRazorPages();
			services.AddServerSideBlazor();
			services.AddHttpContextAccessor();
			services.AddSingleton<XpoDataStoreProviderAccessor>();
			// services.AddScoped<CircuitHandler, CircuitHandlerProxy>();
			services.AddXaf(Configuration, () => Platform.Blazor.NewApplication<TModule>().ToBlazor());
			services.AddXafSecurity(options => {
					options.RoleType = typeof(PermissionPolicyRole);
					options.UserType = UserType();
					options.Events.OnSecurityStrategyCreated = securityStrategy =>
						((SecurityStrategy)securityStrategy).RegisterXPOAdapterProviders();
					options.SupportNavigationPermissionsForTypes = false;
				}).AddExternalAuthentication<HttpContextPrincipalProvider>()
				.AddAuthenticationStandard(options => { options.IsSupportChangePassword = true; });

			services.AddSingleton<IXafApplicationProvider, ApplicationProvider>()
				.AddScoped<IExceptionHandlerService, MyClass>();
			// services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => {
			//  options.LoginPath = "/LoginPage";
			// });
		}

		protected virtual Type UserType() => typeof(PermissionPolicyUser);
	}

	class MyClass : IExceptionHandlerService {
		public void HandleException(Exception exception) {
			throw exception;
		}
	}

	internal class ApplicationProvider : IXafApplicationProvider, IDisposable {
		private readonly IValueManagerStorageContext _valueManagerStorageContext;
		private readonly IXafApplicationFactory _applicationFactory;
		private Lazy<BlazorApplication> _application;
		private readonly IValueManagerStorageContainerInitializer _containerInitializer;

		static ApplicationProvider() => Tracing.NeedContextInformation +=
			(_, e) => {
				IValueManager<string> valueManager = ValueManager.GetValueManager<string>("ApplicationContextId");
				if (!valueManager.CanManageValue)
					return;
				string str = valueManager.Value;
				if (str == null)
					return;
				e.ContextInformation = str;
			};

		public ApplicationProvider(
			IValueManagerStorageContext valueManagerStorageContext,
			IXafApplicationFactory applicationFactory, IValueManagerStorageContainerInitializer containerInitializer) {
			_valueManagerStorageContext = valueManagerStorageContext;
			_applicationFactory = applicationFactory;
			_application = new Lazy<BlazorApplication>(CreateApplication);
			_containerInitializer = containerInitializer;
		}

		public BlazorApplication GetApplication() {
			_containerInitializer.Initialize();
			_valueManagerStorageContext.EnsureStorage();
			return _application.Value;
		}

		private BlazorApplication CreateApplication() {
			ValueManager.GetValueManager<bool>("ApplicationCreationMarker").Value =
				!ValueManager.GetValueManager<bool>("ApplicationCreationMarker").Value
					? true
					: throw new InvalidOperationException(
						"Application has been already created and cannot be created again in current logical call context.");
			ValueManager.GetValueManager<string>("ApplicationContextId").Value = Guid.NewGuid().ToString();
			var application = _applicationFactory.CreateApplication();
			return application;
		}

		void IDisposable.Dispose() {
			if (_application is { IsValueCreated: true })
				_valueManagerStorageContext.RunWithStorage(() => _application.Value.Dispose());
			_application = null;
		}
	}
}