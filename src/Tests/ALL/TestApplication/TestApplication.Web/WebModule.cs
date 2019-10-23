using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.Module;

namespace TestApplication.Web{
    public class WebModule:ModuleBase{
        public WebModule(){
            this.AddModulesFromPath("Xpand.XAF.Modules*.dll");
            this.AddModulesFromPath("DevExpress.ExpressApp*.dll");
            AdditionalExportedTypes.Add(typeof(Customer));
        }
    }
}