using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using DevExpress.ExpressApp.DC;

namespace Xpand.Source.Extensions.XAF.TypesInfo{
    internal static class Extensions{
        public static bool RuntimeMode(this ITypesInfo typeInfo){
            var devProcceses = new[]{".ExpressApp.ModelEditor", "devenv"};
            var processName = Process.GetCurrentProcess().ProcessName;
            var isInProccess = devProcceses.Any(s => processName.IndexOf(s, StringComparison.Ordinal) > -1);
            return !isInProccess && LicenseManager.UsageMode != LicenseUsageMode.Designtime;
        }
    }
}