using System.Reactive;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Jobs {
    [JobProvider]
    public class NotificationJob {
        
        public BlazorApplication Application { get; }

        public NotificationJob() {
        }

        public NotificationJob(BlazorApplication application) => Application = application;

        [JobProvider]
        public async Task Execute(PerformContext context) {
            var containerInitializer = Application.ServiceProvider.GetService<IValueManagerStorageContainerInitializer>();
            // if (((IValueManagerStorageAccessor) containerInitializer)?.Storage == null) {
                // containerInitializer.Initialize();
            // }
	        using var objectSpace = Application.CreateObjectSpace();
	        await Application.JobNotification(context.JobId())
                .SwitchIfEmpty(Unit.Default.ReturnObservable()).ToTask();
        }

    }

}