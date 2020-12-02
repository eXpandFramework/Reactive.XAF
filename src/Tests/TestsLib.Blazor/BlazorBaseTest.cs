using System;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpand.TestsLib.Common;

namespace Xpand.TestsLib.Blazor {
    public class BlazorBaseTest:BaseTest {
        private IHost _webHost;


        static BlazorBaseTest() {
            TestsLib.Common.Extensions.ApplicationType = typeof(TestBlazorApplication);
        }
        public override void Dispose() {
            base.Dispose();
            _webHost.StopAsync().Wait();
        }

        protected BlazorApplication NewBlazorApplication(Type startupType){
            IHostBuilder defaultBuilder = Host.CreateDefaultBuilder();
            
            _webHost = defaultBuilder
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup(startupType))
                .Build();
            _webHost.Start();
            var containerInitializer = _webHost.Services.GetService<IValueManagerStorageContainerInitializer>();
            if (((IValueManagerStorageAccessor) containerInitializer)?.Storage == null) {
                containerInitializer.Initialize();
            }
            var newBlazorApplication = _webHost.Services.GetService<IXafApplicationProvider>()?.GetApplication();
            if (newBlazorApplication != null) {
                newBlazorApplication.ServiceProvider = _webHost.Services;
            }
            return newBlazorApplication;
        }

    }
}