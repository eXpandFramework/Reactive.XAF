using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors.Grid;
using DevExpress.ExpressApp.Blazor.Editors.Grid.Models;
using Hangfire;
using Hangfire.MemoryStorage;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests{
    [NonParallelizable]
    public class JobSchedulerTests:JobSchedulerCommonTest{


        [TestCase(false)]
        [TestCase(true)]
        [XpandTest()]
        public void Customize_Job_Schedule(bool newObject) {
            GlobalConfiguration.Configuration.UseMemoryStorage();
            using var application = JobSchedulerModule().Application;
            var objectSpace = application.CreateObjectSpace();
            
            var scheduledJob = objectSpace.CreateObject<Job>();
            scheduledJob.Id = "test";
            var testObserver = JobService.CustomJobSchedule.Handle().SelectMany(args => args.Instance).Test();
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

        [TestCase(typeof(TestJobDI))]
        [TestCase(typeof(TestJob))]
        [XpandTest()]
        public async Task Inject_Service_Provider_In_JobType_Ctor(Type testJobType) {
            MockHangfire().Test();
            var jobs = TestJob.Jobs.SubscribeReplay();
            using var application = JobSchedulerModule().Application.ToBlazor();
            application.CommitNewJob(testJobType);

            var testJob = await jobs.FirstAsync();

            if (testJobType==typeof(TestJobDI)) {
                testJob.Application.ShouldNotBeNull();
            }
            else {
                testJob.Application.ShouldBeNull();
            }
            
        }

        
        [Test][Apartment(ApartmentState.STA)]
        [XpandTest()]
        public async Task Schedule_Successful_job() {
            MockHangfire().Test();
            using var application = JobSchedulerModule().Application.ToBlazor();
            var observable = JobExecution(WorkerState.Succeeded).SubscribeReplay();
            
            application.CommitNewJob();
            
            var jobState = await observable;
            var objectSpace = application.CreateObjectSpace();
            jobState=objectSpace.GetObjectByKey<JobState>(jobState.Oid);
            var jobWorker = jobState.JobWorker;
            jobWorker.State.ShouldBe(WorkerState.Succeeded);
            
            jobWorker.Executions.Count(state => state.State==WorkerState.Processing).ShouldBe(1);
            jobWorker.Executions.Count(state => state.State==WorkerState.Failed).ShouldBe(0);
            jobWorker.Executions.Count(state => state.State==WorkerState.Succeeded).ShouldBe(1);
            
        }
        
        [TestCase(nameof(TestJob.FailMethodNoRetry),1,1,0)]
        [TestCase(nameof(TestJob.FailMethodRetry),2,1,0)]
        [XpandTest()]
        public async Task Schedule_Failed_Recurrent_job(string methodName,int executions,int failedJobs,int successFullJobs) {
            MockHangfire().Test();
            using var application = JobSchedulerModule().Application.ToBlazor();
            var execute = JobExecution(WorkerState.Failed).SubscribeReplay();
            application.CommitNewJob(methodName:methodName);
            using var objectSpace = application.CreateObjectSpace();

            var jobState = await execute;
            
            jobState=objectSpace.GetObjectByKey<JobState>(jobState.Oid);
            jobState.JobWorker.State.ShouldBe(WorkerState.Failed);
            
        }

        [XpandTest()][Test]
        public async Task Pause_Job() {
            MockHangfire().Test();
            using var application = JobSchedulerModule().Application.ToBlazor();
            var observable = JobExecution(WorkerState.Succeeded).SubscribeReplay();
            var jobsObserver = TestJob.Jobs.Test();
            application.CommitNewJob().Pause();

            var jobState = await observable;

            jobsObserver.ItemCount.ShouldBe(0);
        }
        [XpandTest()][Test]
        public async Task Resume_Job() {
            MockHangfire().Test();
            using var application = JobSchedulerModule().Application.ToBlazor();
            var observable = JobExecution(WorkerState.Succeeded).SubscribeReplay();
            var jobsObserver = TestJob.Jobs.Test();
            application.CommitNewJob().Pause().Resume();

            await observable;

            jobsObserver.ItemCount.ShouldBe(1);
        }
        [XpandTest()][Test]
        public void JobPause_Action() {
            MockHangfire().Test();
            using var application = JobSchedulerModule().Application.ToBlazor();

            var job = application.CommitNewJob();
            var view = application.NewDetailView(job);
            var viewWindow = application.CreateViewWindow();
            viewWindow.SetView(view);

            var action = viewWindow.Action<JobSchedulerModule>().PauseJob();
            action.Active.ResultValue.ShouldBeTrue();
            action.DoExecute(space => new[]{job});
            
            job.IsPaused.ShouldBeTrue();
            view.ObjectSpace.Refresh();

            action.Active.ResultValue.ShouldBeFalse();
            viewWindow.Action<JobSchedulerModule>().ResumeJob().Active.ResultValue.ShouldBeTrue();
            
        }
        [XpandTest()][Test]
        public void JobResume_Action() {
            MockHangfire().Test();
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
            action.DoExecute(space => new[]{job});
            
            job.IsPaused.ShouldBeFalse();
            view.ObjectSpace.Refresh();

            action.Active.ResultValue.ShouldBeFalse();
            viewWindow.Action<JobSchedulerModule>().PauseJob().Active.ResultValue.ShouldBeTrue();
        }

    }
}