using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using TestApplication.Web;

namespace TestApplication.Blazor.Server{
    public class BlazorModule : ModuleBase{
        public BlazorModule(){
            RequiredModuleTypes.Add(typeof(WebModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Blazor.SystemModule.SystemBlazorModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.FileAttachments.Blazor.FileAttachmentsBlazorModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Validation.Blazor.ValidationBlazorModule));
            
        }
    }
}