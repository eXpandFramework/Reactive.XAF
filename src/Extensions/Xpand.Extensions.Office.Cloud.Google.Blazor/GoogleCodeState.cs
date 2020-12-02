using System;
using System.Linq;
using Google.Apis.Auth.OAuth2.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.Blazor;

namespace Xpand.Extensions.Office.Cloud.Google.Blazor{
    public class GoogleCodeStateStartup : IHostingStartup{
        public void Configure(IWebHostBuilder builder) 
            => builder.ConfigureServices(services => {
                services.AddTransient<IStartupFilter, GoogleCodeState>();
            });
    }

    public class GoogleCodeState : IStartupFilter{
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) 
            => app => {
                app.Use(async (context, next2) => {
                    var cultureQuery = context.Request.Query["code"];
                    var code = cultureQuery.FirstOrDefault();
                    if (code != null){
                        var state = context.Request.Query["state"].First();
                        var key = Guid.Parse(state.Substring(0, state.Length - AuthorizationCodeWebApp.StateRandomLength));
                        app.ApplicationServices.GetService<GlobalItems>()?.TryAdd(key, code);
                    }
                    await next2();
                });
                next(app);
            };
    }
}