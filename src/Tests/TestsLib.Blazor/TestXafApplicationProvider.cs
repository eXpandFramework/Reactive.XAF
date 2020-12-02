using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;

namespace Xpand.TestsLib.Blazor {
    public class TestXafApplicationProvider<TModule>:SharedXafApplicationProvider where TModule:ModuleBase, new() {
        private readonly IServiceProvider _serviceProvider;

        public TestXafApplicationProvider(IServiceProvider serviceProvider,
            IValueManagerStorageContainerInitializer containerInitializer) :
            base(serviceProvider, containerInitializer) {
            _serviceProvider = serviceProvider;
        }

        protected override BlazorApplication NewBlazorApplication() {
            var blazorApplication = Platform.Blazor.NewApplication<TModule>().ToBlazor();
            blazorApplication.ServiceProvider = _serviceProvider;
            blazorApplication.AddModule<TModule>();
            return blazorApplication;
        }
    }
}