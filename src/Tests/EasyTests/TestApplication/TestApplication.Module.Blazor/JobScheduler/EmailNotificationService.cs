using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib.Common.BO;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Email;
using Xpand.XAF.Modules.Reactive.Services;

namespace TestApplication.Module.Blazor.JobScheduler {
    public static class EmailNotificationService {
        internal static IObservable<Unit> ConnectEmailNotification(this ApplicationModulesManager manager) {
            return manager.WhenGeneratingModelNodes<IModelNotificationEmailTypes>()
                .Do(types => {
                    var emailType = types.AddNode<IModelNotificationEmailType>();
                    emailType.Type = emailType.Application.BOModel.GetClass(typeof(Product));
                }).ToUnit();
        }

    }
}