using System;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xpand.TestsLib.Blazor;

namespace Xpand.XAF.Modules.ObjectTemplate.Tests.Common {
    public class NotificationStartup : XafHostingStartup<ObjectTemplateModule> {
        public NotificationStartup(IConfiguration configuration) : base(configuration) { }

        public override void ConfigureServices(IServiceCollection services) {
            base.ConfigureServices(services);
            services.AddSingleton<Xpand.TestsLib.Blazor.XpoDataStoreProviderAccessor>(_ => new XpoDataStoreProviderAccessor());
            
        }
    }
    
    public class XpoDataStoreProviderAccessor:Xpand.TestsLib.Blazor.XpoDataStoreProviderAccessor{ }
    
    class NotificationApplicationProvider:TestXafApplicationProvider<ObjectTemplateModule> {
        
        protected override BlazorApplication CreateApplication(IXafApplicationFactory applicationFactory) {
            var blazorApplication = base.CreateApplication(applicationFactory);
            // blazorApplication.ConfigureModel();
            return blazorApplication;
        }

        public NotificationApplicationProvider(IServiceProvider serviceProvider, IValueManagerStorageContainerInitializer containerInitializer) : base(serviceProvider, containerInitializer) { }
    }
}