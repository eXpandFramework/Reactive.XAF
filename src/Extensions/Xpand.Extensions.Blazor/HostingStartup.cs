using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Xpand.Extensions.Blazor {
    public class HostingStartup : IHostingStartup {
        public void Configure(IWebHostBuilder builder) 
            => builder.ConfigureServices(services => {
                services.AddSingleton<GlobalItems>();
                services.AddScoped(sp => {
                    var navigationManager = sp.GetRequiredService<NavigationManager>();
                    return new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };
                });
            });
    }
}