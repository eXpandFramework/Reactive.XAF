using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using Fasterflect;
using NUnit.Framework;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Tests.BO;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Tests.Common {
    static class NotificationTestExtension {
        public static string ScheduledJobId => $"{TestContext.CurrentContext.Test.MethodName}{TestContext.CurrentContext.Test.ID}";
        
        public static JobSchedulerNotificationModule JobSchedulerNotificationModule(this BlazorApplication newBlazorApplication) {
            var module = newBlazorApplication.AddModule<JobSchedulerNotificationModule>(typeof(JSNE).CollectExportedTypesFromAssembly().ToArray());
            newBlazorApplication.Logon();
            using var objectSpace = newBlazorApplication.CreateObjectSpace();
            return module;
        }
        
        public static ObjectStateNotification NewNotificationJob(this IObjectSpace objectSpace,Type objectType=null) {
            objectType ??= typeof(JSNE);
            var notificationJob = objectSpace.CreateObject<ObjectStateNotification>();
            notificationJob.Object = new ObjectType(objectType);
            notificationJob.Id = ScheduledJobId;
            return notificationJob;
        }

        public static IObservable<Unit> NotificationJob(this BlazorApplication application,JobWorker notificationJob) 
            => Unit.Default.Observe().Delay(TimeSpan.FromMilliseconds(300))
                .SelectMany(_ => ((IObservable<Unit>)typeof(NotificationService).Method("JobNotification", Flags.StaticPrivate)
                    .Call(new object[] { application, notificationJob.Id })).Select(unit => unit));

        public static IObservable<XafApplication> CreateExistingObjects<T>(this BlazorApplication application) where T:IJSNE 
            => application.WhenSetupComplete()
                .Do(_ => {
                    var notificationJob = application.CreateObjectSpace().NewNotificationJob();
                    for (int i = 0; i < 2; i++) {
                        var jsne = notificationJob.ObjectSpace.CreateObject<T>();
                        jsne.Name = i.ToString();
                    } 

                    notificationJob.ObjectSpace.CommitChanges();
                });
    }
}