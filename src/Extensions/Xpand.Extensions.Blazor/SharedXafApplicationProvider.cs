#nullable enable
using System;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Xpand.Extensions.Blazor {
    public interface ISharedXafApplicationProvider {
        BlazorApplication Application { get; }
        ISecurityStrategyBase? Security { get; }
    }
    public interface ISharedBlazorApplication {
        bool UseNonSecuredObjectSpaceProvider { get; set; }
    }

    public class SharedXafApplicationProvider : ISharedXafApplicationProvider {
        
        private readonly Lazy<BlazorApplication> _sharedApplication;
        private readonly IServiceProvider _serviceProvider;
        private readonly IValueManagerStorageContainerInitializer _containerInitializer;

        public SharedXafApplicationProvider(IServiceProvider serviceProvider, IValueManagerStorageContainerInitializer containerInitializer) {
            _serviceProvider = serviceProvider;
            _containerInitializer = containerInitializer;
            _sharedApplication = new Lazy<BlazorApplication>(CreateApplication, true);
        }
        
        public ISecurityStrategyBase? Security { get; private set; }

        public BlazorApplication Application => _sharedApplication.Value;
    
        protected BlazorApplication CreateApplication() {
            if (((IValueManagerStorageAccessor) _containerInitializer).Storage == null) {
                _containerInitializer.Initialize();
            }
            return NewBlazorApplication();
        }
    
        protected BlazorApplication NewBlazorApplication() {
            var serviceScope = _serviceProvider.CreateScope();
            var applicationFactory = serviceScope.ServiceProvider.GetRequiredService<IXafApplicationFactory>();
            var blazorApplication = CreateApplication(applicationFactory);
            if (!(blazorApplication is ISharedBlazorApplication sharedBlazorApplication)) {
                throw new NotImplementedException(
                    $"Please implement {typeof(ISharedBlazorApplication)} in your {blazorApplication.GetType().FullName} and use a NonSecuredObjectSpaceProvider when is false.");
            }

            blazorApplication.ServiceProvider = serviceScope.ServiceProvider;
            sharedBlazorApplication.UseNonSecuredObjectSpaceProvider = true;
            Security = blazorApplication.Security;
            blazorApplication.Security = null;
            blazorApplication.Setup();
            return (BlazorApplication)sharedBlazorApplication;
        }

        protected virtual BlazorApplication CreateApplication(IXafApplicationFactory applicationFactory) => applicationFactory.CreateApplication();
    }
}