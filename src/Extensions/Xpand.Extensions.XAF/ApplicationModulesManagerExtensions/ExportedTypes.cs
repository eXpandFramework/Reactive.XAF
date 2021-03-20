using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.XAF.ApplicationModulesManagerExtensions {
    public static partial class ApplicationModulesManagerExtensions {
        public static IEnumerable<Type> ExportedTypes(this DevExpress.ExpressApp.ApplicationModulesManager manager)
            => manager.Modules.SelectMany(m => m.GetExportedTypes().Concat(m.AdditionalExportedTypes).Distinct());
    }
}