using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Xpand.Extensions.Blazor {
    public class HostingStartup : IHostingStartup {
        public void Configure(IWebHostBuilder builder) 
            => builder.ConfigureServices(services => {
                services.AddSingleton<GlobalItems>();
                services.AddSingleton<ISharedXafApplicationProvider,SharedXafApplicationProvider>();
            });
    }
}