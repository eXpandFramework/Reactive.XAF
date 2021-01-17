#nullable enable
using System;
using System.Linq;
using System.Reflection;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Utils;
using Fasterflect;
using Microsoft.Extensions.DependencyInjection;

namespace Xpand.Extensions.Blazor {
    public interface ISharedXafApplicationProvider {
        BlazorApplication Application { get; }
    }
    // public class SharedXafApplicationProvider : ISharedXafApplicationProvider {
    //     private readonly Lazy<BlazorApplication> _sharedApplication;
    //     private readonly IServiceProvider _serviceProvider;
    //     private readonly IValueManagerStorageAccessor _valueManagerStorageAccessor;
    //     private IValueManagerStorage? _sharedApplicationValueManagerStorage;
    //
    //     public SharedXafApplicationProvider(IServiceProvider serviceProvider, IValueManagerStorageAccessor valueManagerStorageAccessor) {
    //         _serviceProvider = serviceProvider;
    //         _valueManagerStorageAccessor = valueManagerStorageAccessor;
    //         _sharedApplication = new Lazy<BlazorApplication>(CreateApplication, true);
    //     }
    //
    //     public BlazorApplication Application => _sharedApplication.Value;
    //
    //     public IValueManagerStorage ApplicationValueManagerStorage => _sharedApplicationValueManagerStorage!;
    //
    //     BlazorApplication CreateApplication() {
    //         _sharedApplicationValueManagerStorage = _valueManagerStorageAccessor.Storage;
    //
    //         //TODO Important - not secured
    //         return NewBlazorApplication();            
    //     }
    //
    //     protected virtual BlazorApplication NewBlazorApplication() {
    //         var applicationModel = CaptionHelper.ApplicationModel;
    //         var assembly = Assembly.GetEntryAssembly()?.GetTypes().First(type => typeof(BlazorApplication).IsAssignableFrom(type));
    //         var app = (BlazorApplication) assembly?.CreateInstance()!; //<- disable security
    //         app.ServiceProvider = _serviceProvider;
    //         app.Setup();
    //         return app;
    //     }
    // }

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
            // var serviceType = AppDomain.CurrentDomain.GetAssemblyType("DevExpress.ExpressApp.Blazor.Services.IValueManagerStorageContainerAccessor");
            // var requiredService = _serviceProvider.GetRequiredService(serviceType);
            //
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