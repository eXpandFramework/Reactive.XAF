using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Blazor.Services;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests {
    [Order(20)]
    public class ChainJobTests:JobSchedulerCommonTest {


        [XpandTest(state:ApartmentState.MTA)][Test]
        public async Task ChainedJob() {
            
            await Observable.Using(() => new Subject<JobState>(),signal => StartJobSchedulerTest(application => application.WhenMainWindowCreated()
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
                        })).ToUnit())
                .MergeToUnit(application.WhenSetupComplete()
                    .SelectMany(_ => application.WhenProviderCommitted<JobState>(emitUpdatingObjectSpace:true)
                        .ToObjects().Where(state => state.State==WorkerState.Succeeded)
                        .Do(signal.OnNext)).IgnoreElements())
                .MergeToUnit(signal.Distinct(state => state.Oid).Skip(1))
                .ReplayFirstTake()));
            //subject.Dispose();
        }
    }
}