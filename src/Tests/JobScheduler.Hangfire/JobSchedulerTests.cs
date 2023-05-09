using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.BO;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests{
    [NonParallelizable]
    public class JobSchedulerTests:JobSchedulerCommonTest{
        public override void Setup() {
            base.Setup();
            Timeout=TimeSpan.FromSeconds(15);
        }

        [TestCase(typeof(TestJob))]
        [TestCase(typeof(TestJobDI))]
        [Test()][Order(0)]
        public async Task Inject_BlazorApplication_In_JobType_Ctor(Type testJobType) {
            using var jobs = TestJob.Jobs.FirstAsync().Test();
            
            await using var application = JobSchedulerModule().Application.ToBlazor();
            
            application.CommitNewJob(testJobType).Trigger(application.ServiceProvider);
            
            var testJob =jobs.AwaitDone(Timeout).Items.First();
            
            if (testJobType==typeof(TestJobDI)) {
                testJob.Provider.ShouldNotBeNull();
            }
            else {
                testJob.Provider.ShouldBeNull();
            }
            await WebHost.StopAsync();
        }
        
        [Test()]
        [XpandTest()][Order(0)]
        public async Task Commit_Objects_NonSecuredProvider() {
            
            await using var application = JobSchedulerModule().Application.ToBlazor();
            using var testObserver = application.WhenProviderObject<JS>().FirstAsync().Test();
            application.CommitNewJob(typeof(TestJobDI),nameof(TestJobDI.CreateObject)).Trigger(application.ServiceProvider);

            testObserver.AwaitDone(Timeout).ItemCount.ShouldBe(1);

            await WebHost.StopAsync();
        }
        [Test()]
        [XpandTest()][Order(0)]
        public async Task Commit_Objects_SecuredProvider() {
            var application = NewBlazorApplication();
            application.AddSecuredProviderModule<JobSchedulerModule>(typeof(JS));

            using var testObserver = application.WhenProviderCommitted<JobState>().ToObjects()
                .FirstAsync(worker => worker.State==WorkerState.Failed)
                .Select(state => state.Reason).Test();
            application.AddNonSecuredType(typeof(Job));
            application.CommitNewJob(typeof(TestJobDI),nameof(TestJobDI.CreateObject)).Trigger(application.ServiceProvider);
            

            testObserver.AwaitDone(Timeout*3).ItemCount.ShouldBe(1);
            var reason = testObserver.Items.First();
            reason.ShouldContain("object is prohibited by security");

            await WebHost.StopAsync();
        }

        [TestCase(nameof(TestJobDI.CreateObjectAnonymous))]
        [TestCase(nameof(TestJobDI.CreateObjectNonSecured))]
        [XpandTest()][Order(0)]
        public async Task Commit_Objects_SecuredProvider_ByPass(string method) {
            var application = NewBlazorApplication();
            application.AddSecuredProviderModule<JobSchedulerModule>(typeof(JS));
            
            
            using var testObserver = application.WhenProviderObject<JobState>()
                .FirstAsync(worker => worker.State==WorkerState.Succeeded)
                .Test();
            application.AddNonSecuredType(typeof(Job));
            application.CommitNewJob(typeof(TestJobDI),method).Trigger(application.ServiceProvider);
            
            testObserver.AwaitDone(Timeout).ItemCount.ShouldBe(1);

            await WebHost.StopAsync();
        }

        [TestCase(false)] 
        [TestCase(true)]
        [XpandTest()][Order(100)]
        public async Task Customize_Job_Schedule(bool newObject) {
            await using var application = JobSchedulerModule().Application.ToBlazor();
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
            
            await WebHost.StopAsync();
        }

        
        [Test()][Apartment(ApartmentState.MTA)]
        [XpandTest()][Order(200)]
        public async Task Inject_PerformContext_In_JobType_Method() {
            var jobs = TestJob.Jobs.FirstAsync().ReplayConnect();
            await using var application = JobSchedulerModule().Application.ToBlazor();
            
            var job = application.CommitNewJob(typeof(TestJob),nameof(TestJob.TestJobId)).Trigger(application.ServiceProvider);

            var testJob = await jobs.Timeout(Timeout);


            testJob.Context.ShouldNotBeNull();
            testJob.Context.JobId().ShouldBe(job.Id);
            var objectSpace = application.CreateObjectSpace();
            job = objectSpace.GetObject(job);
            job.JobMethods.Count.ShouldBeGreaterThan(0);
            
            await WebHost.StopAsync();
        }
        
        
        [Test][Apartment(ApartmentState.STA)]
        [XpandTest()]
        [Order(300)]
        public async Task Schedule_Successful_job() {
            await using var application = JobSchedulerModule().Application.ToBlazor();
            var testObserver = WorkerState.Succeeded.Executed().FirstAsync().ReplayConnect();
            
            application.CommitNewJob().Trigger(application.ServiceProvider);
            
            var jobState = await testObserver.Timeout(Timeout);
            
            var objectSpace = application.CreateObjectSpace();
            jobState=objectSpace.GetObjectByKey<JobState>(jobState.Oid);
            var jobWorker = jobState.JobWorker;
            jobWorker.State.ShouldBe(WorkerState.Succeeded);
            
            jobWorker.Executions.Count(state => state.State==WorkerState.Processing).ShouldBe(1);
            jobWorker.Executions.Count(state => state.State==WorkerState.Failed).ShouldBe(0);
            jobWorker.Executions.Count(state => state.State==WorkerState.Succeeded).ShouldBe(1);
            await WebHost.StopAsync();
        }

        [XpandTest()][Test][Order(Int32.MaxValue)]
        public async Task Pause_Job() {
            await using var application = JobSchedulerModule().Application.ToBlazor();
            using var jobsObserver = TestJob.Jobs.FirstAsync().Test();
            application.CommitNewJob(modify:job => job.SetMemberValue(nameof(Job.IsPaused),true)).Trigger(application.ServiceProvider);

            var itemCount = jobsObserver.AwaitDone(Timeout).ItemCount;
            itemCount.ShouldBe(0);
            await WebHost.StopAsync();
        }

        [XpandTest()][Test][Order(700)]
        public async Task Resume_Job() {
            await using var application = JobSchedulerModule().Application.ToBlazor();
            var jobsCommitObserver = TestJob.Jobs.Timeout(Timeout).FirstAsync().Test();

            application.CommitNewJob( ).Pause().Resume().Trigger(application.ServiceProvider);

            jobsCommitObserver.AwaitDone(Timeout).ItemCount.ShouldBe(1);
            await WebHost.StopAsync();
            
        }

        [XpandTest()][Test][Order(800)]
        public async Task JobPause_Action() {
            await using var application = JobSchedulerModule().Application.ToBlazor();

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
            await WebHost.StopAsync();
        }

        [XpandTest()][Test][Order(900)]
        public void JobResume_Action() {
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

        
        [TestCase(nameof(TestJob.FailMethodRetry),2)]
        [XpandTest()][Order(1000)]
        public async Task Schedule_Failed_Recurrent_job(string methodName,int executions) {
            await using var application = JobSchedulerModule().Application.ToBlazor();
            var testObserver = JobSchedulerService.JobState.FirstAsync(state => state.State==WorkerState.Failed).ReplayConnect();
            application.CommitNewJob(methodName:methodName).Trigger(application.ServiceProvider);

            var observer = await testObserver.Timeout(Timeout*3);

            var objectSpace = application.CreateObjectSpace();
            var jobState = objectSpace.GetObject(observer);
            jobState.JobWorker.ExecutionsCount.ShouldBe(executions);
            await WebHost.StopAsync();
        }
    }
}