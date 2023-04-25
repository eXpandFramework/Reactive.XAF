using System;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Xpo;
using Hangfire;
using Hangfire.MemoryStorage;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.TestsLib.Blazor;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.BO;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common {
    public abstract class JobSchedulerCommonTest : BlazorCommonTest {
        
        public override void Setup() {
            base.Setup();
            GlobalConfiguration.Configuration.UseMemoryStorage(new MemoryStorageOptions());
            
        }

        public override void Dispose() {
            base.Dispose();
            JobStorage.Current = null;
        }

        protected override void ResetXAF() {
            base.ResetXAF();
            XpoTypesInfoHelper.Reset();
        }

        public JobSchedulerModule JobSchedulerModule(params ModuleBase[] modules) {
            var newBlazorApplication = NewBlazorApplication();
            return JobSchedulerModule(newBlazorApplication);
        }

        protected BlazorApplication NewBlazorApplication() {
            var newBlazorApplication = NewBlazorApplication(typeof(JobSchedulerStartup));
            newBlazorApplication.WhenApplicationModulesManager()
                .SelectMany(manager => manager.WhenGeneratingModelNodes<IModelJobSchedulerSources>()
                    .Do(sources => {
                        var source = sources.AddNode<IModelJobSchedulerSource>();
                        source.AssemblyName = GetType().Assembly.GetName().Name;
                    })).FirstAsync().TakeUntilDisposed(newBlazorApplication).Subscribe();
            return newBlazorApplication;
        }

        protected JobSchedulerModule JobSchedulerModule(BlazorApplication newBlazorApplication) {
            var module = newBlazorApplication.AddModule<JobSchedulerModule>(typeof(JS));
            newBlazorApplication.Logon();
            using var objectSpace = newBlazorApplication.CreateObjectSpace();
            return module;
        }

        // protected IObservable<Job> MockHangfire(Type testJobType = null, string testName = null) {
        //     testJobType ??= typeof(TestJobDI);
        //     testName ??= nameof(TestJob.Test);
        //
        //     return JobSchedulerService.CustomJobSchedule.ScheduleImmediate(testJobType.CallExpression(testName));
        // }
    }
}