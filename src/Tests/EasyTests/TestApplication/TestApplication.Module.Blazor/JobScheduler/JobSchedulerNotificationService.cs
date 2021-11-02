using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using TestApplication.Module.Common;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib.Common.BO;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification;
using Xpand.XAF.Modules.Reactive.Services;

namespace TestApplication.Module.Blazor.JobScheduler {
    public static class JobSchedulerNotificationService {
        internal static IObservable<Unit> ConnectJobSchedulerNotification(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => {
                    var useNonSecuredObjectSpaceProvider = ((ISharedBlazorApplication)application).UseNonSecuredObjectSpaceProvider;
                    if (!useNonSecuredObjectSpaceProvider) {
                        return application.WhenNotification<Product>().TakeUntilDisposed(application)
                            .SelectMany(t => t.objects.Do(product => {
                                var testTask = t.job.ObjectSpace.CreateObject<TestTask>();
                                testTask.Subject = product.Id.ToString();
                                t.job.ObjectSpace.CommitChanges();
                            }))
                            .ToUnit();
                    }
                    return Observable.Empty<Unit>();
                })
                .ToUnit()
        ;
    }
}