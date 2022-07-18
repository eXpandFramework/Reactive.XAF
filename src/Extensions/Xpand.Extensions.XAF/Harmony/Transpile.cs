using System.Reflection;
using HarmonyLib;
using Xpand.Extensions.Harmony;

namespace Xpand.Extensions.XAF.Harmony {
    public static partial class HarmonyExtensions {
        public static void Transpile(this HarmonyMethod harmonyMethod, MethodInfo method, bool onlyRuntime)
            => method.Patch(harmonyMethod.Transpile, onlyRuntime);
    }
}