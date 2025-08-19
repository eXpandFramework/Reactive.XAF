using System;
using System.Reflection;
using DevExpress.ExpressApp.Model.Core;
using HarmonyLib;
using Xpand.Extensions.Harmony;

namespace Xpand.Extensions.XAF.Harmony {
    public static partial class HarmonyExtensions {
        static void Patch(this MethodInfo method,Action<MethodInfo> patch, bool onlyRuntime = true) {
            
                 patch(method);
        }

        public static void PreFix(this HarmonyMethod harmonyMethod, MethodInfo method, bool onlyRuntime )
            => method.Patch(harmonyMethod.PreFix, onlyRuntime);
    }
}