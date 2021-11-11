using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using TestApplication.Module.Common;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib.Common.BO;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Services;

namespace TestApplication.Module.Blazor.JobScheduler {
    public static class JobSchedulerNotificationService {
        internal static IObservable<Unit> ConnectJobSchedulerNotification(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.WhenNotification<Product>()
                .SelectMany(t => t.objects.Do(product => {
                        var testTask = t.worker.ObjectSpace.CreateObject<TestTask>();
                        testTask.Subject = product.Id.ToString();
                        t.worker.ObjectSpace.CommitChanges();
                    }).ToNowObservable()
                    .Finally(() => t.worker.NotifyFinish())
                .ToUnit()));

        private static IObservable<(ObjectStateNotification job, Product[] objects)> CreateTask(IObservable<(ObjectStateNotification job, Product[] objects)> source) 
            => source.SelectMany(t => t.objects.Do(product => {
                var testTask = t.job.ObjectSpace.CreateObject<TestTask>();
                testTask.Subject = product.Id.ToString();
                t.job.ObjectSpace.CommitChanges();
            }).ToNowObservable().To(t));
    }
}