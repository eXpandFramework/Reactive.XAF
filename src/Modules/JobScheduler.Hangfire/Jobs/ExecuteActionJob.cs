using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Jobs {
    [JobProvider]
    public class ExecuteActionJob {
        
        public IServiceProvider ServiceProvider { get; }

        public ExecuteActionJob() {
        }

        [ActivatorUtilitiesConstructor]
        public ExecuteActionJob(IServiceProvider provider) => ServiceProvider = provider;

        [JobProvider]
        public async Task<bool> Execute(PerformContext context)
            => await ServiceProvider.RunWithStorageAsync(application => application.ExecuteAction(context.JobId())).ToObservable().To(true);
    }

}