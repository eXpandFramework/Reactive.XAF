using System.Collections.Concurrent;
using System.Reflection;
using DevExpress.ExpressApp.Model.Core;
using HarmonyLib;

namespace Xpand.Extensions.XAF.AppDomainExtensions {
    public static partial class AppDomainExtensions {
	    private static Harmony _harmony;
	    private static readonly ConcurrentDictionary<string,MethodInfo> PatchedMethods = new();
	    public static MethodInfo PatchWith(this MethodInfo method, HarmonyMethod prefix = null,
		    HarmonyMethod postFix = null, HarmonyMethod transpiler = null,HarmonyMethod finalizer = null) {
		    _harmony ??= new Harmony("XAF");
		    if (DesignerOnlyCalculator.IsRunTime) {
			    var methodName = $"{method.DeclaringType?.FullName}{method.Name}";
			    if (!PatchedMethods.TryGetValue(methodName, out _)) {
				    PatchedMethods.TryAdd(methodName, method);
				    _harmony.Patch(method, prefix, postFix, transpiler,finalizer);
			    }
		    }
		    return method;
	    }
    }
}