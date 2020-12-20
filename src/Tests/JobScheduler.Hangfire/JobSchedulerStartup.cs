using Microsoft.Extensions.Configuration;
using Xpand.TestsLib.Blazor;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests {
    public class JobSchedulerStartup : XafHostingStartup<JobSchedulerModule> {
        public JobSchedulerStartup(IConfiguration configuration) : base(configuration) { }
    }
}