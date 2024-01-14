using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model.Core;

namespace TestApplication.Module.Blazor{


    public class TestBlazorModule : ModuleBase{
        public TestBlazorModule(){
            // RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Blazor.SystemModule.SystemBlazorModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.FileAttachments.Blazor.FileAttachmentsBlazorModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Validation.Blazor.ValidationBlazorModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Office.Blazor.OfficeBlazorModule));
            RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.JobScheduler.Hangfire.JobSchedulerModule));
            RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.JobSchedulerNotificationModule));
            
        }


    }
}