using System;
using DevExpress.ExpressApp.Model.Core;
using HarmonyLib;
using JetBrains.Annotations;

namespace Xpand.Extensions.XAF.AppDomainExtensions {
    public static partial class AppDomainExtensions {
        private static Harmony _harmony;

        [PublicAPI]
        public static void Patch(this AppDomain appDomain, Action<Harmony> patch) {
            // if (DesignerOnlyCalculator.IsRunTime) {
                _harmony ??= new Harmony("XAF");
                patch(_harmony);
            // }
        }
    }
}