using System;
using DevExpress.ExpressApp.Model.Core;
using HarmonyLib;
using JetBrains.Annotations;

namespace Xpand.Extensions.XAF.AppDomainExtensions {
    public static partial class AppDomainExtensions {
        private static Harmony _harmony;

        [PublicAPI]
        public static Harmony Patch(this AppDomain appDomain, Action<Harmony> patch) {
	        _harmony ??= new Harmony("XAF");
	        if (DesignerOnlyCalculator.IsRunTime) {
		        patch(_harmony);
	        }
	        return _harmony;
        }
    }
}