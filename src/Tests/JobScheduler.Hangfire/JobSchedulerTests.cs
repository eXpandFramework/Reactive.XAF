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
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests{
    [NonParallelizable]
    public class JobSchedulerTests:JobSchedulerBaseTest{


        [TestCase(false)]
        [TestCase(true)]
        [XpandTest()]
        public void Customize_Job_Schedule(bool newObject) {
            
            using var application = JobSchedulerModule().Application;
            var objectSpace = application.CreateObjectSpace();
            
            var scheduledJob = objectSpace.CreateObject<ScheduledJob>();
            scheduledJob.Id = "test";
            var testObserver = ScheduledJobService.CustomJobSchedule.Handle().SelectMany(args => args.Instance).Test();
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
        // [TestCase(typeof(TestJob))]
        // [XpandTest()]
        [Apartment(ApartmentState.STA)]
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
            var observable = JobExecution(ScheduledJobState.Succeeded).SubscribeReplay();
            
            application.CommitNewJob();
            
            var jobExecution = await observable;
            var objectSpace = application.CreateObjectSpace();
            jobExecution=objectSpace.GetObjectByKey<JobState>(jobExecution.Oid);
            var job = jobExecution.Job;
            job.State.ShouldBe(ScheduledJobState.Succeeded);
            job.ScheduledJob.Executions.ShouldBe(1);
            job.ScheduledJob.FailedJobs.ShouldBe(0);
            job.ScheduledJob.SuccessFullJobs.ShouldBe(1);
            
        }
        
        [TestCase(nameof(TestJob.FailMethodNoRetry),1,1,0)]
        [TestCase(nameof(TestJob.FailMethodRetry),2,1,0)]
        [XpandTest()]
        public async Task Schedule_Failed_Recurrent_job(string methodName,int executions,int failedJobs,int successFullJobs) {
            MockHangfire(testName:methodName).FirstAsync().Test();
            using var application = JobSchedulerModule().Application.ToBlazor();
            var execute = JobExecution(ScheduledJobState.Failed).SubscribeReplay();
            application.CommitNewJob(methodName:methodName);
            using var objectSpace = application.CreateObjectSpace();

            var jobExecution = await execute;
            
            jobExecution=objectSpace.GetObjectByKey<JobState>(jobExecution.Oid);
            jobExecution.Job.State.ShouldBe(ScheduledJobState.Failed);
            jobExecution.Job.ScheduledJob.Executions.ShouldBe(executions);
            jobExecution.Job.ScheduledJob.FailedJobs.ShouldBe(failedJobs);
            jobExecution.Job.ScheduledJob.SuccessFullJobs.ShouldBe(successFullJobs);
        }

        private static  IObservable<JobState> JobExecution(ScheduledJobState lastState) 
            => ScheduledJobService.JobExecution.FirstAsync(t => t.State == ScheduledJobState.Enqueued).IgnoreElements()
                .Concat(ScheduledJobService.JobExecution.FirstAsync(t => t.State == ScheduledJobState.Processing).IgnoreElements())
                .Concat(ScheduledJobService.JobExecution.FirstAsync(t => t.State == lastState))
                .FirstAsync();
    }
}