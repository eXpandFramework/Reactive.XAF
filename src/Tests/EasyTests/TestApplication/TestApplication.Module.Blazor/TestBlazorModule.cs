using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model.Core;
using TestApplication.Module.Blazor.JobScheduler;
using TestApplication.Module.Blazor.JobScheduler.Notification;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace TestApplication.Module.Blazor{

    public class TestBlazorModule : ModuleBase,IWebModule{
        public TestBlazorModule(){
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Blazor.SystemModule.SystemBlazorModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.FileAttachments.Blazor.FileAttachmentsBlazorModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Validation.Blazor.ValidationBlazorModule));
            RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.JobScheduler.Hangfire.JobSchedulerModule));
            RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.JobSchedulerNotificationModule));
            RequiredModuleTypes.Add(typeof(TestApplicationModule));
        }

        public override void AddGeneratorUpdaters(ModelNodesGeneratorUpdaters updaters) {
            updaters.Add(new JoSchedulerSourceUpdater());
        }

        public override void Setup(ApplicationModulesManager moduleManager) {
            base.Setup(moduleManager);
            moduleManager.ConnectJobScheduler()
                .Merge(moduleManager.ConnectJobSchedulerNotification())
                .Subscribe(this);
        }
    }
}