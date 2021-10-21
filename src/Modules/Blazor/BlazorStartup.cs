using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xpand.XAF.Modules.Blazor.Editors;

namespace Xpand.XAF.Modules.Blazor {
    public class BlazorStartupFilter : IStartupFilter {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) 
            => app => {
                app.UseMiddleware<UploadFileMiddleware>();
                next(app);
            };
    }

    public class BlazorStartup : IHostingStartup{
        public void Configure(IWebHostBuilder builder) 
            => builder.ConfigureServices(services 
                => services.AddSingleton<IStartupFilter, BlazorStartupFilter>());
    }
}