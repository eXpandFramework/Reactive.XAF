using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp.Blazor;
using Hangfire.Server;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Jobs {
    [JobProvider]
    public class ExecuteActionJob {
        
        public BlazorApplication Application { get; }

        public ExecuteActionJob() {
        }

        public ExecuteActionJob(BlazorApplication application) => Application = application;

        [JobProvider]
        public async Task Execute(PerformContext context) 
            => await Application.ExecuteAction(context.JobId());
    }

}