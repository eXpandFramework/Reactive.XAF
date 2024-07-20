using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {
        public static ModuleBase Module(this XafApplication application, object value) => application.Modules.Module(value);

        public static ModuleBase Module(this IEnumerable<ModuleBase> applicationModules,object value) 
            => applicationModules.FirstOrDefault(moduleBase
                => moduleBase.GetType().Assembly == value.GetType().Assembly);
    }
}