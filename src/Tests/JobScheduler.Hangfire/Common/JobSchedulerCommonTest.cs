using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Xpo;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Blazor;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common {
    
    public abstract class JobSchedulerCommonTest : BlazorCommonTest {
        
        public override void Setup() {
            base.Setup();
            XpoTypesInfoHelper.GetXpoTypeInfoSource().Reset();
            XafTypesInfo.HardReset();
            GlobalConfiguration.Configuration.UseMemoryStorage(new MemoryStorageOptions());
            
        }

        public override void Dispose() {
            JobStorage.Current = null;
        }

        protected override void ResetXAF() {
            // base.ResetXAF();
            // XpoTypesInfoHelper.Reset();
        }

        protected IObservable<Unit> StartJobSchedulerTest(Func<BlazorApplication, IObservable<Unit>> test,Func<BlazorApplication,IObservable<Unit>> beforeSetup=null,Func<WebHostBuilderContext, TestStartup> startupFactory=null,TimeSpan? timeOut=null) 
            => StartTest(test,BeforeSetup(),configureWebHostBuilder:ConfigureWebHostBuilder(),startupFactory:context => startupFactory?.Invoke(context),timeOut:timeOut,configureServices:ConfigureServices);

        private void ConfigureServices(IServiceCollection services) {
            
        }

        private Action<IWebHostBuilder> ConfigureWebHostBuilder() 
            => builder => builder.UseSetting(WebHostDefaults.HostingStartupAssembliesKey, GetType().Assembly.GetName().Name);

        private Func<BlazorApplication, IObservable<Unit>> BeforeSetup()
            => application => application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.WhenGeneratingModelNodes<IModelJobSchedulerSources>().Take(1)
                    .Do(sources => sources.AddNode<IModelJobSchedulerSource>().AssemblyName = GetType().Assembly.GetName().Name)).ToUnit()
                .Merge(application.ServiceProvider.WhenApplicationStopping()
                    .SelectMany(unit => application.GetRequiredService<IEnumerable<IHostedService>>().OfType<BackgroundJobServerHostedService>().ToObservable()
                        .SelectMany(service => service.StopAsync(CancellationToken.None).ToObservable()
                            .Select(_ => unit))).ToUnit().IgnoreElements());
        


    }
}