using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.ApplicationModulesManagerExtensions{
    public static partial class ApplicationModulesManagerExtensions{
        public static IEnumerable<DevExpress.ExpressApp.XafApplication> WhereApplication(this DevExpress.ExpressApp.ApplicationModulesManager manager) 
            => manager.Modules.OfType<SystemModule>().Select(module => module.Application).WhereNotDefault().Select(application => application);

        public static DevExpress.ExpressApp.XafApplication Application(this DevExpress.ExpressApp.ApplicationModulesManager manager) 
            => manager.Modules.OfType<SystemModule>().First().Application;
    }
}