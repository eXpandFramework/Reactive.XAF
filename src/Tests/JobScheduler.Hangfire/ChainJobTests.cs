using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using Hangfire;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.Reactive.Utility;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests {
    public class ChainJobTests:JobSchedulerCommonTest {

        [TestCase(true,1,nameof(ChainJob.TestChainJob))]
        [TestCase(false,0,nameof(ChainJob.TestChainJob))]
        [TestCase(false,0,nameof(ChainJob.TestVoidChainJob))]
        [Apartment(ApartmentState.STA)]
        [XpandTest()][Order(400)]
        public void ChainedJob(bool result,int chainExecCount,string methodName) {
            ChainJob.Result = result;
            using var application = JobSchedulerModule().Application.ToBlazor();
            using var parentJobObserver = WorkerState.Succeeded.Executed().Test();

            application.CommitNewJob(typeof(ChainJob), methodName, job => {
                var chainJob = job.ObjectSpace.CreateObject<BusinessObjects.ChainJob>();
                chainJob.Job = chainJob.ObjectSpace.GetObject(application.CommitNewJob(typeof(ChainJob),nameof(ChainJob.TestChainJob),job1 => {
                    job1.Id = nameof(ChainJob);
                    job1.CronExpression = job1.ObjectSpace.GetObjectsQuery<CronExpression>()
                        .First(expression => expression.Name == nameof(Cron.Never));
                }));
                job.ChainJobs.Add(chainJob);
            }).Trigger();

            var chainJobObserver = WorkerState.Succeeded.Executed(job => !job.ChainJobs.Any()).FirstAsync().Test();
            var jobState = parentJobObserver.AwaitDone(Timeout).Items.First();

            chainJobObserver.AwaitDone(Timeout).ItemCount.ShouldBe(chainExecCount);
            
            var objectSpace = application.CreateObjectSpace();
            jobState=objectSpace.GetObjectByKey<JobState>(jobState.Oid);
            var jobWorker = jobState.JobWorker;
            jobWorker.State.ShouldBe(WorkerState.Succeeded);
        }

    }
}