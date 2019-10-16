using System.Linq;
using DevExpress.ExpressApp.SystemModule;

namespace Xpand.Extensions.XAF.ApplicationModulesManager{
    public static partial class ApplicationModulesManagerExtensions{
        public static DevExpress.ExpressApp.XafApplication Application(this DevExpress.ExpressApp.ApplicationModulesManager manager){
            return manager.Modules.OfType<SystemModule>().First().Application;
        }

    }
}