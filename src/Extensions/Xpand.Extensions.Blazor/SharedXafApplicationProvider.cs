#nullable enable
using System;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Xpand.Extensions.Blazor {
    public interface ISharedXafApplicationProvider {
        BlazorApplication Application { get; }
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
                blazorApplication.ServiceProvider = _serviceProvider;
                blazorApplication.Setup();
                return blazorApplication;
            }


            throw new NotImplementedException();
        }
    }
}