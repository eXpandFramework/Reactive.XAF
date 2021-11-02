using System;
using System.Diagnostics;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.AmbientContext;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Base;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
[assembly: HostingStartup(typeof(HostingStartup))]
namespace Xpand.TestsLib.Blazor {
    public abstract class XafHostingStartup<TModule> where TModule : ModuleBase, new() {
        public IConfiguration Configuration { get; }

        protected XafHostingStartup(IConfiguration configuration) => Configuration = configuration;

        public void Configure(IApplicationBuilder app) => app.UseXaf();

        public virtual void ConfigureServices(IServiceCollection services) {
            Tracing.Initialize(AppDomain.CurrentDomain.ApplicationPath(),TraceLevel.Verbose.ToString());
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddHttpContextAccessor();
            services.TryAddScoped(typeof(IXafApplicationFactory), s => {
                IXafApplicationFactory factory = new XafApplicationFactory(NewApplication);
                factory = new XafApplicationServiceProviderInitializer(factory, s);
                return new XafApplicationSecurityInitializer(factory, s.GetService<ISecurityInitializer>());
            });
            services.AddXaf<TestBlazorApplication>(Configuration);
            BlazorApplication NewApplication() => Platform.Blazor.NewApplication<TModule>().ToBlazor();
            // services.AddSingleton<IXafApplicationFactory>(provider => new XafApplicationFactory(() => {
            //     var blazorApplication = NewApplication();
            //     blazorApplication.ServiceProvider=provider;
            //     return blazorApplication;
            // }));
            
            
            services
                .AddSingleton<IXafApplicationProvider,ApplicationProvider>()
                .AddSingleton<ISharedXafApplicationProvider, TestXafApplicationProvider<TModule>>()
                .AddSingleton<XpoDataStoreProviderAccessor>()
                .AddScoped<IExceptionHandlerService,MyClass>();

        }
        internal sealed class XafApplicationFactory : IXafApplicationFactory {
            private readonly Func<BlazorApplication> _createApplication;
            public XafApplicationFactory(Func<BlazorApplication> createApplication) {
                _createApplication = createApplication ?? throw new ArgumentNullException(nameof(createApplication));
            }
            public BlazorApplication CreateApplication() => _createApplication();
        }

        internal sealed class XafApplicationSecurityInitializer : IXafApplicationFactory {
            private readonly IXafApplicationFactory _original;
            private readonly ISecurityInitializer _xafSecurityInitializer;
            public XafApplicationSecurityInitializer(IXafApplicationFactory original, ISecurityInitializer xafSecurityInitializer = null) {
                _original = original ?? throw new ArgumentNullException(nameof(original));
                _xafSecurityInitializer = xafSecurityInitializer;
            }
            public BlazorApplication CreateApplication() {
                BlazorApplication application = _original.CreateApplication();
                _xafSecurityInitializer?.InitializeSecurity(application);
                return application;
            }
        }

        internal sealed class XafApplicationServiceProviderInitializer : IXafApplicationFactory {
            private readonly IXafApplicationFactory _original;
            private readonly IServiceProvider _serviceProvider;
            public XafApplicationServiceProviderInitializer(IXafApplicationFactory original, IServiceProvider serviceProvider) {
                _original = original ?? throw new ArgumentNullException(nameof(original));
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            }
            public BlazorApplication CreateApplication() {
                BlazorApplication application = _original.CreateApplication();
                ITypesInfo typesInfo = _serviceProvider.GetRequiredService<ITypesInfo>();
                if(application.TypesInfo != null && !application.TypesInfo.Equals(typesInfo)) {
                    throw new InvalidOperationException($"The application ({application.GetType().FullName}) uses a wrong ITypesInfo instance. Ensure that ctor(ITypesInfo) exists.");
                }
                application.ServiceProvider = _serviceProvider;
                return application;
            }
        }

        class MyClass:IExceptionHandlerService {
            public void HandleException(Exception exception) {
                throw exception;
            }
        }


        // internal sealed class XafApplicationFactory : IXafApplicationFactory {
        //     private readonly Func<BlazorApplication> _createApplication;
        //     public XafApplicationFactory(Func<BlazorApplication> createApplication) 
        //         => _createApplication = createApplication ?? throw new ArgumentNullException(nameof(createApplication));
        //     public BlazorApplication CreateApplication() => _createApplication();
        // }d

    }

    public class ApplicationProvider:IXafApplicationProvider,IDisposable {
        private readonly IValueManagerStorageContext _valueManagerStorageContext;
		private readonly IXafApplicationFactory _applicationFactory;
		// private readonly ApplicationLogonManager applicationLogonManager;
		private Lazy<BlazorApplication> _application;
		static ApplicationProvider() {
			Tracing.NeedContextInformation += (_, e) => {
				IValueManager<string> applicationContextId = ValueManager.GetValueManager<string>("ApplicationContextId");
				if(applicationContextId.CanManageValue && applicationContextId.Value is { } id) {
					e.ContextInformation = id;
				}
			};
		}

		public ApplicationProvider(IValueManagerStorageContext valueManagerStorageContext,
			IXafApplicationFactory applicationFactory) {
			_valueManagerStorageContext = valueManagerStorageContext;
			_applicationFactory = applicationFactory;
            _application = new Lazy<BlazorApplication>(CreateApplication);
		}

		public BlazorApplication GetApplication() {
			_valueManagerStorageContext.EnsureStorage();
			return _application.Value;
		}

		private BlazorApplication CreateApplication() {
			if(ValueManager.GetValueManager<bool>("ApplicationCreationMarker").Value) {
				throw new InvalidOperationException("Application has been already created and cannot be created again in current logical call context.");
			}
			ValueManager.GetValueManager<bool>("ApplicationCreationMarker").Value = true;
			ValueManager.GetValueManager<string>("ApplicationContextId").Value = Guid.NewGuid().ToString();
			var app = _applicationFactory.CreateApplication();
			// app.Setup();
            return app;
		}

		void IDisposable.Dispose() {
			if(_application is { IsValueCreated: true }) {
				_valueManagerStorageContext.RunWithStorage(() => {
					_application.Value.Dispose();
				});
			}
			_application = null;
		}

    }
}