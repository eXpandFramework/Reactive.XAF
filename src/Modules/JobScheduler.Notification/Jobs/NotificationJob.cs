using System;
using System.Threading.Tasks;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Jobs {
    
    public class NotificationJob {
        
        public IServiceProvider ServiceProvider { get; }

        public NotificationJob() {
        }

        [ActivatorUtilitiesConstructor]
        public NotificationJob(IServiceProvider provider) => ServiceProvider = provider;


        public async Task<bool> Execute(PerformContext context) 
            => await ServiceProvider.RunWithStorageAsync(application => application
                .JobNotification(context.BackgroundJob.Id).To(true));
    }

}