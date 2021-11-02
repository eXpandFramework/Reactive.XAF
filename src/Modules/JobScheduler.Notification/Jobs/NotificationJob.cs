using System.Reactive;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using DevExpress.ExpressApp.Blazor;
using Hangfire.Server;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Jobs {
    
    public class NotificationJob {
        
        public BlazorApplication Application { get; }

        public NotificationJob() {
        }

        public NotificationJob(BlazorApplication application) => Application = application;


        public async Task Execute(PerformContext context) {
            await Application.JobNotification(context.JobId())
                .SwitchIfEmpty(Unit.Default.ReturnObservable()).ToTask();
        }

    }

}