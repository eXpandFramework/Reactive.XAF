using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Xpand.Extensions.Blazor {
    public class HostingStartup : IHostingStartup {
        public void Configure(IWebHostBuilder builder) 
            => builder.ConfigureServices(services => {
                services.AddSingleton<SingletonItems>();
                services.AddScoped<ScopedItems>();
                services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(sp.GetRequiredService<NavigationManager>().BaseUri) });
            });
    }
}