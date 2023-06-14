using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.Extensions.XAF.ApplicationModulesManagerExtensions{
    public static partial class ApplicationModulesManagerExtensions{
        public static IEnumerable<XafApplication> WhereApplication(this ApplicationModulesManager manager) 
            => manager.Modules.OfType<SystemModule>().Select(module => module.Application).WhereNotDefault().Select(application => application);

        public static IServiceProvider ServiceProvider(this ApplicationModulesManager manager)
            => manager.Application()?.ServiceProvider ?? manager.ControllersManager.ServiceProvider();
        
        public static XafApplication Application(this ApplicationModulesManager manager) 
            => manager.Modules.OfType<SystemModule>().First().Application;
        
        public static T Module<T>(this ApplicationModulesManager manager) where T:ModuleBase 
            => manager.Modules.OfType<T>().FirstOrDefault();
    }
}