using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.SystemModule;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.ApplicationModulesManagerExtensions{
    public static partial class ApplicationModulesManagerExtensions{
        public static IEnumerable<XafApplication> WhereApplication(this ApplicationModulesManager manager) 
            => manager.Modules.OfType<SystemModule>().Select(module => module.Application).WhereNotDefault().Select(application => application);

        public static XafApplication Application(this ApplicationModulesManager manager) 
            => manager.Modules.OfType<SystemModule>().First().Application;
        
        public static T Module<T>(this ApplicationModulesManager manager) where T:ModuleBase 
            => manager.Modules.OfType<T>().FirstOrDefault();
    }
}