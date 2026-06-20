using System;
using System.Reactive;
using DevExpress.ExpressApp.Blazor;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xpand.TestsLib.Blazor;

namespace Xpand.XAF.Modules.Email.Tests.Common{
    public abstract class EmailModuleTestsBaseTest:BlazorCommonTest {
        protected IObservable<Unit> StartEmailTest(Func<BlazorApplication, IObservable<Unit>> test,TimeSpan? timeOut=null) 
            => StartTest<TestStartup>(test,configureWebHostBuilder:ConfigureWebHostBuilder(),timeOut:timeOut,configureServices:ConfigureServices);
        
        private Action<IWebHostBuilder> ConfigureWebHostBuilder() 
            => builder => {
                builder.UseSetting(WebHostDefaults.HostingStartupAssembliesKey, GetType().Assembly.GetName().Name);
                builder.UseStaticWebAssets();
            };
        
        private void ConfigureServices(IServiceCollection services) {
            
        }
    }
}