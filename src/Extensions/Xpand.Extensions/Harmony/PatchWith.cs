using System;
using System.Collections.Concurrent;
using System.Reflection;
using HarmonyLib;

namespace Xpand.Extensions.Harmony {
    public static partial class HarmonyExtensions {
	    private static HarmonyLib.Harmony _harmony;
	    private static readonly ConcurrentDictionary<string,MethodInfo> PatchedMethods = new();
	    public static void PatchWith(this MethodInfo method, HarmonyMethod prefix = null,
		    HarmonyMethod postFix = null, HarmonyMethod transpiler = null,HarmonyMethod finalizer = null) {
            _harmony ??= new HarmonyLib.Harmony("XAF");
		    var methodName = $"{method.DeclaringType?.FullName}{method.Name}";
		    if (!PatchedMethods.TryGetValue(methodName, out _)) {
			    PatchedMethods.TryAdd(methodName, method);
                try {
                    _harmony.Patch(method, prefix, postFix, transpiler,finalizer);
                }
                catch (Exception e) {
                    Console.WriteLine(e);
                    
                }
		    }
	    }
    }
}