using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp.Xpo;
using Hangfire;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Blazor.Services;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.BO;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests {
    [Order(0)]
    public class JobSchedulerTests:JobSchedulerCommonTest {
        
        [TestCase(typeof(TestJob),false)]
        [TestCase(typeof(TestJobDI),true)]
        [XpandTest(state:ApartmentState.MTA)]
        public async Task Inject_BlazorApplication_In_JobType_Ctor(Type jobType,bool provider) 
            => await StartJobSchedulerTest(application => application.AssertTriggerJob(jobType,
                    nameof(TestJob.TestJobId), true).IgnoreElements()
                .MergeToUnit(TestJob.Jobs.Take(1).If(_ => provider,
                    job => job.Provider.Observe().WhenNotDefault(),
                    job => job.Provider.Observe().WhenDefault()).ToUnit()).ReplayFirstTake()
        );
        
        [Test()]
        [XpandTest(state:ApartmentState.MTA)]
        [Order(200)]
        public async Task Inject_PerformContext_In_JobType_Method()
            => await StartJobSchedulerTest(application => application.AssertTriggerJob(typeof(TestJob),
                    nameof(TestJob.TestJobId), true).IgnoreElements()
                .MergeToUnit(TestJob.Jobs.WhenNotDefault(job => job.Context).Take(1).ToUnit()).ReplayFirstTake());


        [Test()]
        [XpandTest(state:ApartmentState.MTA)]
        public async Task Commit_Objects_NonSecuredProvider()
            => await StartJobSchedulerTest(application => application.AssertTriggerJob(typeof(TestJobDI),
                    nameof(TestJobDI.CreateObject),true).IgnoreElements()
                .MergeToUnit(application.WhenSetupComplete().SelectMany(_ => application.WhenProviderCommitted<JS>(emitUpdatingObjectSpace:true))
                    .Select(t => t).Take(1).ToUnit()).ReplayFirstTake()
                .ToUnit().Select(unit => unit), startupFactory: context => new TestStartup(context.Configuration,startup => startup.AddObjectSpaceProviders));
        
        
        [Test()]
        [XpandTest(state:ApartmentState.MTA)]
        public async Task Commit_Objects_SecuredProvider()
            => await StartJobSchedulerTest(application =>
                TestTracing.Handle<UserFriendlyObjectLayerSecurityException>().Take(1).IgnoreElements()
                    .MergeToUnit(application.AssertTriggerJob(typeof(TestJobDI), nameof(TestJobDI.CreateObject), false).IgnoreElements())
                    .MergeToUnit(JobSchedulerService.JobState.Where(worker => worker.State == WorkerState.Failed && worker.Reason.Contains("object is prohibited by security") ))
                    .ReplayFirstTake()
        );
        
        [TestCase(nameof(TestJobDI.CreateObjectAnonymous))]
        [TestCase(nameof(TestJobDI.CreateObjectNonSecured))]
        [XpandTest(state:ApartmentState.MTA)]
        public async Task Commit_Objects_SecuredProvider_ByPass(string method) 
            => await StartJobSchedulerTest(application =>
                application.AssertTriggerJob(typeof(TestJobDI), method, false).IgnoreElements()
                    .MergeToUnit(JobSchedulerService.JobState.Where(state => state.State==WorkerState.Succeeded))
                    
                    .ReplayFirstTake()
        );

        // [Test()]
        // FAIL 
        [XpandTest(state:ApartmentState.MTA)]
        public async Task Customize_Job_Schedule()
            => await StartJobSchedulerTest(application => application.AssertJobListViewNavigation()
                .SelectMany(window => window.CreateJob(typeof(TestJobDI), nameof(TestJobDI.TestJobId))).ToUnit()
                .Zip(JobSchedulerService.CustomJobSchedule.Handle().SelectMany(args => args.Instance).Take(1)).ToSecond()
                .ToUnit().ReplayFirstTake());
        
        [Test]
        [XpandTest(state:ApartmentState.MTA)]
        public async Task Schedule_Successful_job() 
            => await StartJobSchedulerTest(application => application.AssertTriggerJob(typeof(TestJobDI), nameof(TestJobDI.TestJobId),true).IgnoreElements()
                .MergeToUnit(WorkerState.Succeeded.Executed().Where(state => state.JobWorker.State==WorkerState.Succeeded)
                    .Where(jobState => jobState.JobWorker.Executions.DistinctBy(state => state.State).Count() == 3 && jobState.JobWorker.Executions.Select(state => state.State)
                        .All(state => new[]{WorkerState.Enqueued,WorkerState.Processing, WorkerState.Succeeded}.Contains(state))).Take(1)
                    .Select(state => state))
                .ReplayFirstTake());
        
        // [XpandTest(state:ApartmentState.MTA)]
        // [Test]
        public async Task Trigger_Paused_Job()
            => await StartJobSchedulerTest(application
                => application.WhenMainWindowCreated()
                    .SelectMany(_ => {
                        var objectSpace = application.CreateObjectSpace();
                        var job = objectSpace.CreateObject<Job>();
                        job.Id = nameof(Trigger_Paused_Job);
                        job.JobType = new ObjectType(typeof(TestJobDI));
                        job.JobMethod = new ObjectString(nameof(TestJobDI.TestJobId));
                        job.IsPaused = true;
                        job.CommitChanges();
                        job.Trigger(application.ServiceProvider);
                        return application.Navigate(typeof(Job))
                            .SelectMany(frame => frame.AssertListViewHasObject<Job>()
                                .SelectMany(_ => frame.ListViewProcessSelectedItem()))
                            .Zip(application.WhenTabControl(typeof(Job))).ToSecond().Do(model => model.ActiveTabIndex=1)
                            .Assert();
                    }).IgnoreElements()
                    .MergeToUnit(application.WhenSetupComplete().SelectMany(_ => application.WhenProviderCommitted<JobState>(emitUpdatingObjectSpace:true).ToObjects()
                        .Where(worker => worker.State==WorkerState.Skipped).Take(1)))
                    .Select(t => t)
                    .ToUnit().ReplayFirstTake());
        
        // [XpandTest(state:ApartmentState.MTA)][Test]
        public async Task Trigger_Resume_Job()
            => await StartJobSchedulerTest(application
                => application.WhenMainWindowCreated()
                    .SelectMany(_ => {
                        var objectSpace = application.CreateObjectSpace();
                        var job = objectSpace.CreateObject<Job>();
                        job.Id = nameof(Trigger_Resume_Job);
                        job.JobType = new ObjectType(typeof(TestJobDI));
                        job.JobMethod = new ObjectString(nameof(TestJobDI.TestJobId));
                        job.IsPaused = true;
                        job.CommitChanges();
                        job.IsPaused = false;
                        job.CommitChanges();
                        job.Trigger(application.ServiceProvider);
                        return application.Navigate(typeof(Job))
                            .SelectMany(frame => frame.AssertListViewHasObject<Job>()
                                .SelectMany(_ => frame.ListViewProcessSelectedItem()))
                            .Zip(application.WhenTabControl(typeof(Job))).ToSecond().Do(model => model.ActiveTabIndex = 1)
                            .Zip(application.AssertListViewHasObject<JobWorker>(worker
                                => worker.State == WorkerState.Succeeded))
                            .Assert();
                    }).ToUnit().ReplayFirstTake());
        
        [XpandTest(state:ApartmentState.MTA)]
        [Test]
        public async Task JobPause_Action() 
            => await StartJobSchedulerTest(application
                => application.WhenMainWindowCreated()
                    .SelectMany(_ => {
                        var objectSpace = application.CreateObjectSpace();
                        var job = objectSpace.CreateObject<Job>();
                        job.Id = nameof(Trigger_Paused_Job);
                        job.JobType = new ObjectType(typeof(TestJobDI));
                        job.JobMethod = new ObjectString(nameof(TestJobDI.TestJobId));
                        job.CommitChanges();
                        return application.Navigate(typeof(Job))
                            .SelectMany(frame => frame.AssertListViewHasObject<Job>()
                                .SelectMany(_ => frame.AssertSimpleAction(nameof(JobSchedulerService.PauseJob))
                                    .SelectMany(action => action.Trigger(action.WhenDeactivated().Take(1)
                                        .SelectMany(_ => frame.View.ToListView().Objects<Job>().Where(job1 => job1.IsPaused))
                                        .Take(1)
                                        .Select(t => t)))))
                            .Assert().Select(t => t);
                    }).ToUnit().ReplayFirstTake());
        [XpandTest(state:ApartmentState.MTA)]
        // [Test]
        public async Task JobPause_Action1() {
            await StartJobSchedulerTest(application
                => application.ServiceProvider.WhenApplicationStopping()
                    .SelectMany(unit => application.GetRequiredService<IEnumerable<IHostedService>>().OfType<BackgroundJobServerHostedService>().ToObservable()
                        .SelectMany(service => service.StopAsync(CancellationToken.None).ToObservable()
                            .Select(_ => unit))).ToUnit().IgnoreElements()
                    .ReplayFirstTake(), timeOut: 6.Seconds());
        }

        // [XpandTest(state:ApartmentState.MTA)][Test]
        public async Task JobResume_Action() 
            => await StartJobSchedulerTest(application
                => application.WhenMainWindowCreated()
                    .SelectMany(_ => {
                        var objectSpace = application.CreateObjectSpace();
                        var job = objectSpace.CreateObject<Job>();
                        job.Id = nameof(Trigger_Paused_Job);
                        job.JobType = new ObjectType(typeof(TestJobDI));
                        job.JobMethod = new ObjectString(nameof(TestJobDI.TestJobId));
                        job.IsPaused = true;
                        job.CommitChanges();
                        return application.Navigate(typeof(Job))
                            .SelectMany(frame => frame.AssertListViewHasObject<Job>()
                                .SelectMany(_ => frame.AssertSimpleAction(nameof(JobSchedulerService.ResumeJob))
                                    .SelectMany(action => action.Trigger(action.WhenDeactivated().Take(1)
                                        .Zip(frame.AssertListViewHasObject<Job>(job1 => !job1.IsPaused)).Take(1)
                                        .Select(t => t)))))
                            .Assert().Select(t => t);
                    }).ToUnit().ReplayFirstTake());
        
        
        [XpandTest(state:ApartmentState.MTA)][Test]
        public async Task Schedule_Failed_Recurrent_job() {
            await StartJobSchedulerTest(application
                => application.WhenMainWindowCreated()
                    .SelectMany(_ => TestTracing.Handle<NotImplementedException>().Take(1)
                        .MergeToUnit(application.AssertTriggerJob(typeof(TestJobDI), nameof(TestJobDI.FailMethodRetry), false))
                        .MergeToUnit(application.WhenTabControl(typeof(Job)).Do(model => model.ActiveTabIndex = 1).Take(1)))
                    .IgnoreElements()
                    .MergeToUnit(1.Seconds().Interval().TakeUntilDisposed(application)
                        .SelectMany(_ => application.WhenProviderObject<JobWorker>(ObjectModification.All)
                        .Where(worker => worker.ExecutionsCount==2).Take(1)))
                    .ReplayFirstTake()
                );
            
        }

    }
    
}