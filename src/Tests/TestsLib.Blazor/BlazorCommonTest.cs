using System;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpand.TestsLib.Common;

namespace Xpand.TestsLib.Blazor {
    public class BlazorCommonTest:CommonTest {
        protected IHost WebHost;


        static BlazorCommonTest() {
            TestsLib.Common.Extensions.ApplicationType = typeof(TestBlazorApplication);
        }
        public override void Dispose() {
            base.Dispose();
            WebHost.Dispose();
        }

        protected BlazorApplication NewBlazorApplication(Type startupType){
            IHostBuilder defaultBuilder = Host.CreateDefaultBuilder();
            
            WebHost = defaultBuilder
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup(startupType))
                .Build();
            WebHost.Start();
            var containerInitializer = WebHost.Services.GetService<IValueManagerStorageContainerInitializer>();
            if (((IValueManagerStorageAccessor) containerInitializer)?.Storage == null) {
                containerInitializer.Initialize();
            }
            var newBlazorApplication = WebHost.Services.GetService<IXafApplicationProvider>()?.GetApplication();
            if (newBlazorApplication != null) {
                newBlazorApplication.ServiceProvider = WebHost.Services;
            }
            return newBlazorApplication;
        }

    }
}