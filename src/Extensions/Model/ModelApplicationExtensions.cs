using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using DevExpress.ExpressApp.Model.Core;
using Fasterflect;

namespace DevExpress.XAF.Extensions.Model{
    public static class ModelApplicationExtensions {
//        public static bool IsRuntime(this ModelApplicationBase application){
//            if (!DesignerOnlyCalculator.IsRunTime)
//            var devProcceses = new[]{".ExpressApp.ModelEditor", "devenv"};
//            var processName = Process.GetCurrentProcess().ProcessName;
//            var isInProccess = devProcceses.Any(s => processName.IndexOf(s, StringComparison.Ordinal) > -1);
//            return !isInProccess && LicenseManager.UsageMode != LicenseUsageMode.Designtime;
//        }

        public static List<ModelNode> GetLayers(this ModelApplicationBase application) {
            return ((List<ModelNode>) application.GetPropertyValue("Layers"));
        }

        public static void InsertLayer(this ModelApplicationBase application, int index,ModelApplicationBase layer) {
            application.CallMethod("InsertLayerAtInternal", layer, index);
        }
    }
}