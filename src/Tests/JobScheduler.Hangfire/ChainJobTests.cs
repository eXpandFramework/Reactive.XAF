using System.Linq;
using System.Reactive;
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
            var subject = new Subject<JobState>();
            await subject.Use(signal => StartJobSchedulerTest(application => Unit.Default.Observe().ReplayFirstTake()));
        }
    }
}