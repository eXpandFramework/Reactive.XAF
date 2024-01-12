using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Xpo;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Hosting;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib.Blazor;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.BO;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.NewDirectory1;
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

        public JobSchedulerModule JobSchedulerModule(params ModuleBase[] modules) {
            var newBlazorApplication = NewBlazorApplication();
            return JobSchedulerModule(newBlazorApplication);
        }
        protected IObservable<Unit> StartJobSchedulerTest(Func<BlazorApplication, IObservable<Unit>> test,Func<BlazorApplication,IObservable<Unit>> beforeSetup=null,Func<WebHostBuilderContext, TestStartup> startupFactory=null) 
            => StartTest(test,BeforeSetup(),configureWebHostBuilder:ConfigureWebHostBuilder(),startupFactory:context => startupFactory?.Invoke(context));

        private Action<IWebHostBuilder> ConfigureWebHostBuilder() 
            => builder => builder.UseSetting(WebHostDefaults.HostingStartupAssembliesKey, GetType().Assembly.GetName().Name);

        private Func<BlazorApplication, IObservable<Unit>> BeforeSetup()
            => application => application.WhenApplicationModulesManager()
                .SelectMany(manager => manager.WhenGeneratingModelNodes<IModelJobSchedulerSources>().Take(1)
                    .Do(sources => sources.AddNode<IModelJobSchedulerSource>().AssemblyName = GetType().Assembly.GetName().Name)).ToUnit();
        
        protected BlazorApplication NewBlazorApplication() {
            var newBlazorApplication = NewBlazorApplication(typeof(JobSchedulerStartup));
            newBlazorApplication.WhenApplicationModulesManager()
                .SelectMany(manager => manager.WhenGeneratingModelNodes<IModelJobSchedulerSources>()
                    .Do(sources => {
                        var source = sources.AddNode<IModelJobSchedulerSource>();
                        source.AssemblyName = GetType().Assembly.GetName().Name;
                    })).TakeFirst().TakeUntilDisposed(newBlazorApplication).Subscribe();
            return newBlazorApplication;
        }

        protected JobSchedulerModule JobSchedulerModule(BlazorApplication newBlazorApplication) {
            var module = newBlazorApplication.AddModule<JobSchedulerModule>(typeof(JS));
            newBlazorApplication.Logon();
            using var objectSpace = newBlazorApplication.CreateObjectSpace();
            return module;
        }

    }
}