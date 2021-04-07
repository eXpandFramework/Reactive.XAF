using System;
using DevExpress.ExpressApp.Blazor.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.Blazor;
using Xpand.TestsLib.Blazor;

[assembly: HostingStartup(typeof(HostingStartup))]
[assembly: HostingStartup(typeof(Xpand.XAF.Modules.Blazor.BlazorStartup))]

namespace Xpand.XAF.Modules.Reactive.Rest.Tests.Common {
    public class RestStartup : XafHostingStartup<RestModule> {
        public RestStartup(IConfiguration configuration) : base(configuration) { }

        public override void ConfigureServices(IServiceCollection services) {
            base.ConfigureServices(services);
            services.AddSingleton<ISharedXafApplicationProvider, RestApplicationProvider>();
        }
    }

    class RestApplicationProvider:TestXafApplicationProvider<RestModule> {
        public RestApplicationProvider(IServiceProvider serviceProvider,
            IValueManagerStorageContainerInitializer containerInitializer) :
            base(serviceProvider, containerInitializer) { }
    }
}