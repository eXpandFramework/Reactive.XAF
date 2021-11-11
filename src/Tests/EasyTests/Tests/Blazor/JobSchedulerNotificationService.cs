using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.EasyTest.Framework;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib.Common.BO;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using Xpand.TestsLib.EasyTest.Commands.ActionCommands;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.BusinessObjects;

namespace Web.Tests {
    public static class JobSchedulerNotificationService {
        public static IObservable<Unit> TestJobSchedulerNotification(this ICommandAdapter adapter) {
            adapter.CreateJob();
            adapter.CreateProduct();
            adapter.TriggerJob();
            adapter.Execute(new NavigateCommand("Default.Test Task"));
            return adapter.Execute(() => {
                adapter.Execute(new ActionCommand(Actions.Refresh));
                adapter.Execute(new SelectObjectsCommand(nameof(DevExpress.Persistent.BaseImpl.Task.Subject), new []{"1"}));
            })
            .Do(_ => {
                adapter.Execute(new ActionDeleteCommand());
                adapter.Execute(new NavigateCommand("Default.Product"));
                adapter.Execute(new SelectObjectsCommand(nameof(Product.ProductName), new []{nameof(JobSchedulerNotificationService)}));
                adapter.Execute(new ActionDeleteCommand());
            });
        }

        private static void TriggerJob(this ICommandAdapter adapter) {
            adapter.Execute(new NavigateCommand("JobScheduler.Job"));
            adapter.Execute(new SelectObjectsCommand<Job>(notification => notification.Id, new[] { nameof(JobSchedulerNotificationService) }));
            adapter.Execute(new ActionCommand(nameof(Xpand.XAF.Modules.JobScheduler.Hangfire.JobSchedulerService.Trigger)));
        }

        private static void CreateProduct(this ICommandAdapter adapter) {
            adapter.Execute(new NavigateCommand($"Default.{nameof(Product)}"));
            adapter.Execute(new ActionCommand(Actions.New));
            adapter.Execute(new FillObjectViewCommand<Product>((product => product.Id, 1.ToString()),
                (product => product.ProductName, nameof(JobSchedulerNotificationService))));
            adapter.Execute(new ActionCommand(Actions.Save));
        }

        private static void CreateJob(this ICommandAdapter adapter) {
            adapter.Execute(new NavigateCommand("JobScheduler.Job"));
            adapter.Execute(new ActionCommand(Actions.New.ToString(),nameof(ObjectStateNotification)));
            adapter.Execute(new FillObjectViewCommand<ObjectStateNotification>((job => job.Id, nameof(JobSchedulerNotificationService)),(job => job.Object, nameof(Product))));
            adapter.Execute(new ActionCommand(Actions.Save));
        }
    }
}