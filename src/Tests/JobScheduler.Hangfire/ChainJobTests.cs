using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Blazor.Services;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests {
    public class ChainJobTests:JobSchedulerCommonTest {


        [XpandTest(state:ApartmentState.MTA)][Test]
        public async Task ChainedJob() 
            => await StartJobSchedulerTest(application => application.WhenMainWindowCreated()
                .SelectMany(_ => application.AssertJobListViewNavigation()
                    .SelectMany(window => window.CreateJob(typeof(ChainJob), nameof(ChainJob.TestChainJob),false)
                        .Zip(application.WhenTabControl(typeof(Job),selectedTab:1).Take(1)).ToFirst()
                        .SelectMany(frame => {
                            var parentJob = (Job)frame.View.CurrentObject;
                            var chainJob = frame.View.ObjectSpace.CreateObject<BusinessObjects.ChainJob>();
                            parentJob.ChainJobs.Add(chainJob);
                            var childJob = parentJob.ObjectSpace.CreateObject<Job>();
                            childJob.JobType = childJob.JobTypes.First(type => type.Type == typeof(TestJob));
                            childJob.JobMethod = childJob.JobMethods.First(s => s.Name == nameof(TestJob.Test));
                            childJob.Id = nameof(ChainJob);
                            chainJob.Job = childJob;

                            return frame.SaveAction().Trigger(frame.SimpleAction(nameof(JobSchedulerService.TriggerJob)).Trigger()).To(frame)
                                .IgnoreElements();
                        })).ToUnit()
                    .MergeToUnit(application.AssertListViewHasObject<JobWorker>(worker => worker.State==WorkerState.Succeeded)
                        .SelectMany(_ => application.Navigate(typeof(Job))
                            .SelectMany(_ => application.AssertListViewHasObject<Job>(job => job.Id==nameof(ChainJob))
                                .SelectMany(frame2 => frame2.ListViewProcessSelectedItem()
                                    .Zip(application.WhenTabControl(typeof(Job),selectedTab:1).Take(1))
                                    .Zip(application.AssertListViewHasObject<JobWorker>(worker => worker.State==WorkerState.Succeeded))))))
                ).ReplayFirstTake()
        );
    }
}