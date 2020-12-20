using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using Hangfire;
using Hangfire.MemoryStorage;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.Reactive.Utility;
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
            MockHangfire(testName:methodName).FirstAsync().Test();
            using var application = JobSchedulerModule().Application.ToBlazor();
            var execute = JobExecution(WorkerState.Failed).SubscribeReplay();
            application.CommitNewJob(methodName:methodName);
            using var objectSpace = application.CreateObjectSpace();

            var jobState = await execute;
            
            jobState=objectSpace.GetObjectByKey<JobState>(jobState.Oid);
            jobState.JobWorker.State.ShouldBe(WorkerState.Failed);
            
        }

        private static  IObservable<JobState> JobExecution(WorkerState lastState) 
            => JobService.JobExecution.FirstAsync(t => t.State == WorkerState.Enqueued).IgnoreElements()
                .Concat(JobService.JobExecution.FirstAsync(t => t.State == WorkerState.Processing).IgnoreElements())
                .Concat(JobService.JobExecution.FirstAsync(t => t.State == lastState))
                .FirstAsync();
    }
}