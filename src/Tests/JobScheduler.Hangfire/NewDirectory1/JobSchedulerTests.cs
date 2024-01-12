using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Xpo;
using NUnit.Framework;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.BO;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.NewDirectory1 {
    public class JobSchedulerTests:JobSchedulerCommonTest {
        
        [TestCase(typeof(TestJob),false)]
        [TestCase(typeof(TestJobDI),true)]
        public async Task Inject_BlazorApplication_In_JobType_Ctor(Type jobType,bool provider) 
            => await StartJobSchedulerTest(application => application.AssertTriggerJob(jobType,
                nameof(TestJob.TestJobId), true).IgnoreElements()
                .MergeToUnit(TestJob.Jobs.Take(1).If(_ => provider,
                    job => job.Provider.Observe().WhenNotDefault(),
                    job => job.Provider.Observe().WhenDefault()).ToUnit()).ReplayFirstTake()
        );


        [Test()][Order(1)]
        // [XpandTest(state:ApartmentState.MTA)][Order(0)]
        public async Task Commit_Objects_NonSecuredProvider()
            => await StartJobSchedulerTest(application => application.AssertTriggerJob(typeof(TestJobDI),
                    nameof(TestJobDI.CreateObject),true).IgnoreElements()
                .MergeToUnit(application.WhenSetupComplete().SelectMany(xafApplication => application.WhenProviderCommitted<JS>(emitUpdatingObjectSpace:true))
                    .Select(t => t).Take(1).ToUnit()).ReplayFirstTake()
                .ToUnit().Select(unit => unit), startupFactory: context => new TestStartup(context.Configuration,startup => startup.AddObjectSpaceProviders));

        [Test]
        public async Task MethodName() {
            await StartJobSchedulerTest(application => application.Observe().ReplayFirstTake().ToUnit());
        }

        
        [Test()][Order(0)]
        // [XpandTest(state:ApartmentState.MTA)]
        public async Task Commit_Objects_SecuredProvider()
            => await StartJobSchedulerTest(application =>
                TestTracing.Handle<UserFriendlyObjectLayerSecurityException>().IgnoreElements()
                    .MergeToUnit(application.AssertTriggerJob(typeof(TestJobDI), nameof(TestJobDI.CreateObject), false).IgnoreElements())
                    .MergeToUnit(application.WhenTabControl<DxFormLayoutTabPagesModel>(typeof(Job))
                        .Do(model => model.ActiveTabIndex = 1).IgnoreElements())
                    .MergeToUnit(application.AssertListViewHasObject<JobWorker>(worker
                        => worker.State == WorkerState.Failed && worker.LastState.Reason.Contains("object is prohibited by security")))
                    .ReplayFirstTake()
        );
    }
    
}