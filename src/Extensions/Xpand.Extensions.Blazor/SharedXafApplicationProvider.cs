#nullable enable
using System;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Security;
using JetBrains.Annotations;
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

        [NotNull]
        public ISecurityStrategyBase? Security { get; private set; }

        public BlazorApplication Application => _sharedApplication.Value;
    
        protected BlazorApplication CreateApplication() {
            if (((IValueManagerStorageAccessor) _containerInitializer).Storage == null) {
                _containerInitializer.Initialize();
            }
            return NewBlazorApplication();
        }
    
        protected virtual BlazorApplication NewBlazorApplication() {
            using var serviceScope = _serviceProvider.CreateScope();
            var applicationFactory = serviceScope.ServiceProvider.GetService<IXafApplicationFactory>();
            if (applicationFactory != null) {
                var blazorApplication = applicationFactory.CreateApplication();
                if (!(blazorApplication is ISharedBlazorApplication)) {
                    throw new NotImplementedException(
                        $"Please implement {typeof(ISharedBlazorApplication)} in your {blazorApplication.GetType().FullName} and use a NonSecuredObjectSpaceProvider when is false.");
                }

                blazorApplication.ServiceProvider = _serviceProvider;
                ((ISharedBlazorApplication) blazorApplication).UseNonSecuredObjectSpaceProvider = true;
                Security = blazorApplication.Security;
                blazorApplication.Security = null;
                blazorApplication.Setup();
                return blazorApplication;
            }
    
    
            throw new NotImplementedException();
        }
    }
}