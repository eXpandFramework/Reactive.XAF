using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.MemoryStorage.Database;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests{
	[NonParallelizable]
    public class JobSchedulerTests:JobSchedulerCommonTest{
        public override void Setup() {
            base.Setup();
            GlobalConfiguration.Configuration.UseMemoryStorage(new MemoryStorageOptions(),new Data());
        }

        public override void Dispose() {
            base.Dispose();
            JobStorage.Current = null;
        }

        [TestCase(typeof(TestJobDI))]
        [TestCase(typeof(TestJob))]
        [XpandTest()][Order(0)]
        public void Inject_BlazorApplication_In_JobType_Ctor(Type testJobType) {
            using var testObserver = MockHangfire().Test();
            using var jobs = TestJob.Jobs.FirstAsync().Test();
            using var application = JobSchedulerModule().Application.ToBlazor();
            application.CommitNewJob(testJobType);

            var testJob = jobs.AwaitDone(Timeout).Items.First();

            if (testJobType==typeof(TestJobDI)) {
                testJob.Application.ShouldNotBeNull();
            }
            else {
                testJob.Application.ShouldBeNull();
            }
        }

        [TestCase(false)] 
        [TestCase(true)]
        [XpandTest()][Order(100)]
        public void Customize_Job_Schedule(bool newObject) {
            
            using var application = JobSchedulerModule().Application;
            var objectSpace = application.CreateObjectSpace();
            
            var scheduledJob = objectSpace.CreateObject<Job>();
            scheduledJob.Id = "test";
            using var testObserver = JobSchedulerService.CustomJobSchedule.Handle().SelectMany(args => args.Instance).Test();
            objectSpace.CommitChanges();
            
            testObserver.ItemCount.ShouldBe(1);                           
            testObserver.Items.Last().ShouldBe(scheduledJob);
            if (!newObject) {
                scheduledJob.Id = "t";
                objectSpace.CommitChanges();
                testObserver.ItemCount.ShouldBe(2);
                testObserver.Items.Last().ShouldBe(scheduledJob);
            }
        }

        
        [Test()]
        [XpandTest()][Order(200)]
        public void Inject_PerformContext_In_JobType_Method() {
            using var testObserver =MockHangfire().Test();
            using var jobs = TestJob.Jobs.FirstAsync().Test();
            using var application = JobSchedulerModule().Application.ToBlazor();
            
            var job = application.CommitNewJob(typeof(TestJob),nameof(TestJob.TestJobId));
            
            var testJob = jobs.AwaitDone(Timeout).Items.First();

            testJob.Context.ShouldNotBeNull();
            testJob.Context.JobId().ShouldBe(job.Id);
            var objectSpace = application.CreateObjectSpace();
            job = objectSpace.GetObject(job);
            job.JobMethods.Count.ShouldBeGreaterThan(0);            
        }
        
        
        [Test][Apartment(ApartmentState.STA)]
        [XpandTest()][Order(300)]
        public void Schedule_Successful_job() {
            using var mockObserver =MockHangfire().Test();
            using var application = JobSchedulerModule().Application.ToBlazor();
            using var testObserver = WorkerState.Succeeded.Executed().Test();
            
            application.CommitNewJob();
            
            
            var jobState = testObserver.AwaitDone(Timeout).Items.First();
            var objectSpace = application.CreateObjectSpace();
            jobState=objectSpace.GetObjectByKey<JobState>(jobState.Oid);
            var jobWorker = jobState.JobWorker;
            jobWorker.State.ShouldBe(WorkerState.Succeeded);
            
            jobWorker.Executions.Count(state => state.State==WorkerState.Processing).ShouldBe(1);
            jobWorker.Executions.Count(state => state.State==WorkerState.Failed).ShouldBe(0);
            jobWorker.Executions.Count(state => state.State==WorkerState.Succeeded).ShouldBe(1);
            
        }
        
        [Test][Apartment(ApartmentState.STA)]
        [XpandTest()][Order(400)]
        public void ChainedJob() {
            using var mockObserver =MockHangfire().Test();
            using var application = JobSchedulerModule().Application.ToBlazor();
            using var parentJobObserver = WorkerState.Succeeded.Executed().Test();

            application.CommitNewJob(modify: job => {
                var chainJob = job.ObjectSpace.CreateObject<BusinessObjects.ChainJob>();
                chainJob.Job = chainJob.ObjectSpace.GetObject(application.CommitNewJob(typeof(ChainJob),modify:job1 => {
                    job1.Id = nameof(ChainJob);
                    job1.CronExpression = job1.ObjectSpace.GetObjectsQuery<CronExpression>()
                        .First(expression => expression.Name == nameof(Cron.Never));
                }));
                job.ChainJobs.Add(chainJob);
            });

            using var chainJobObserver = WorkerState.Succeeded.Executed().Test();
            var jobState = parentJobObserver.AwaitDone(Timeout).Items.First();

            chainJobObserver.AwaitDone(Timeout);
            
            var objectSpace = application.CreateObjectSpace();
            jobState=objectSpace.GetObjectByKey<JobState>(jobState.Oid);
            var jobWorker = jobState.JobWorker;
            jobWorker.State.ShouldBe(WorkerState.Succeeded);
        }
        
       

        [XpandTest()][Test][Order(600)]
        public async Task Pause_Job() {
            using var mockObserver =MockHangfire().Test();
            using var application = JobSchedulerModule().Application.ToBlazor();
            using var testObserver = WorkerState.Succeeded.Executed().Test();
            using var jobsObserver = TestJob.Jobs.Test();
            var job = application.CommitNewJob();
            jobsObserver.AwaitDone(Timeout);
            
            var jobsObserver2 = TestJob.Jobs.FirstAsync().Timeout(Timeout).ReplayConnect();
            job.Pause();
            job.Trigger();

            await Should.ThrowAsync<TimeoutException>(() => jobsObserver2.ToTask());
        }

        [XpandTest()][Test][Order(700)]
        public void Resume_Job() {
            using var mockObserver =MockHangfire().Test();
            using var application = JobSchedulerModule().Application.ToBlazor();
            
            using var jobsCommitObserver = TestJob.Jobs.FirstAsync().Test();
            var job = application.CommitNewJob();
            jobsCommitObserver.AwaitDone(Timeout);
            job.Pause();
            using var finishSuccess = WorkerState.Succeeded.Executed().FirstAsync().Test();
            
            job.Resume();

            finishSuccess.AwaitDone(Timeout);

            jobsCommitObserver.ItemCount.ShouldBe(1);
        }

        [XpandTest()][Test][Order(800)]
        public void JobPause_Action() {
            using var mockObserver =MockHangfire().Test();
            using var application = JobSchedulerModule().Application.ToBlazor();

            var job = application.CommitNewJob();
            var view = application.NewDetailView(job);
            var viewWindow = application.CreateViewWindow();
            viewWindow.SetView(view);

            var action = viewWindow.Action<JobSchedulerModule>().PauseJob();
            action.Active.ResultValue.ShouldBeTrue();
            action.DoExecute(_ => new[]{job});
            
            job.IsPaused.ShouldBeTrue();
            view.ObjectSpace.Refresh();

            action.Active.ResultValue.ShouldBeFalse();
            viewWindow.Action<JobSchedulerModule>().ResumeJob().Active.ResultValue.ShouldBeTrue();
            
        }

        [XpandTest()][Test][Order(900)]
        public void JobResume_Action() {
            using var mockObserver =MockHangfire().Test();
            using var application = JobSchedulerModule().Application.ToBlazor();

            var job = application.CommitNewJob();
            var view = application.NewDetailView(job);
            var viewWindow = application.CreateViewWindow();
            viewWindow.SetView(view);

            var action = viewWindow.Action<JobSchedulerModule>().ResumeJob();
            action.Active.ResultValue.ShouldBeFalse();
            job.Pause();
            view.ObjectSpace.Refresh();
            action.Enabled.ResultValue.ShouldBeTrue();
            action.DoExecute(_ => new[]{job});
            
            job.IsPaused.ShouldBeFalse();
            view.ObjectSpace.Refresh();

            action.Active.ResultValue.ShouldBeFalse();
            viewWindow.Action<JobSchedulerModule>().PauseJob().Active.ResultValue.ShouldBeTrue();
        }

        // [TestCase(nameof(TestJob.FailMethodNoRetry),1)]
        [TestCase(nameof(TestJob.FailMethodRetry),2)]
        [XpandTest()][Order(1000)]
        public async Task Schedule_Failed_Recurrent_job(string methodName,int executions) {
            using var mockObserver =MockHangfire().Test();
            using var application = JobSchedulerModule().Application.ToBlazor();
            var testObserver = JobSchedulerService.JobState.FirstAsync(state => state.State==WorkerState.Failed).ReplayConnect();
            application.CommitNewJob(methodName:methodName);

            var observer = await testObserver;
            
            var objectSpace = application.CreateObjectSpace();
            var jobState = objectSpace.GetObject(observer);
            jobState.JobWorker.ExecutionsCount.ShouldBe(executions);
        }
    }
}