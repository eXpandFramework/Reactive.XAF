using System.Reflection;
using HarmonyLib;
using Xpand.Extensions.Harmony;

namespace Xpand.Extensions.XAF.Harmony {
    public static partial class HarmonyExtensions {
        public static void Finalize(this HarmonyMethod harmonyMethod, MethodInfo method, bool onlyRuntime )
            => method.Patch(harmonyMethod.Finalize, onlyRuntime);
    }
}