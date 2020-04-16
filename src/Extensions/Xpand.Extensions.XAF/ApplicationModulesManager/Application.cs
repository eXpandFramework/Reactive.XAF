using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Linq;

namespace Xpand.Extensions.XAF.ApplicationModulesManager{
    public static partial class ApplicationModulesManagerExtensions{
        public static IEnumerable<DevExpress.ExpressApp.XafApplication> WhereApplication(this DevExpress.ExpressApp.ApplicationModulesManager manager){
            return manager.Modules.OfType<SystemModule>().Select(module => module.Application).WhereNotDefault();
        }

        public static DevExpress.ExpressApp.XafApplication Application(this DevExpress.ExpressApp.ApplicationModulesManager manager){
            return manager.Modules.OfType<SystemModule>().First().Application;
        }

    }
}