using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.Persistent.Base;

namespace TestApplication.Blazor.Server {
    public class SharedXafApplicationProvider {
        private readonly Lazy<BlazorApplication> _sharedApplication;
        private readonly IServiceProvider _serviceProvider;
        private readonly IValueManagerStorageAccessor _valueManagerStorageAccessor;
        private IValueManagerStorage _sharedApplicationValueManagerStorage;

        public SharedXafApplicationProvider(IServiceProvider serviceProvider, IValueManagerStorageAccessor valueManagerStorageAccessor) {
            _serviceProvider = serviceProvider;
            _valueManagerStorageAccessor = valueManagerStorageAccessor;
            _sharedApplication = new Lazy<BlazorApplication>(CreateApplication, true);
        }

        public XafApplication SharedApplication => _sharedApplication.Value;

        public IValueManagerStorage SharedApplicationValueManagerStorage => _sharedApplicationValueManagerStorage;

        private BlazorApplication CreateApplication() {
            if(ValueManager.GetValueManager<bool>("ApplicationCreationMarker").Value) {
                throw new InvalidOperationException("Application has been already created and cannot be created again in current logical call context.");
            }
            ValueManager.GetValueManager<bool>("ApplicationCreationMarker").Value = true;
            _sharedApplicationValueManagerStorage = _valueManagerStorageAccessor.Storage;

            //TODO Important - not secured
            var app = new ServerBlazorApplication() {ServiceProvider = _serviceProvider}; //<- disable security
            app.Setup();
            return app;
        }
    }
}