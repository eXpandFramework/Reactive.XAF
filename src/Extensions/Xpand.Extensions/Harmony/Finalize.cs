using System.Reflection;
using HarmonyLib;

namespace Xpand.Extensions.Harmony {
    public static partial class HarmonyExtensions {
        public static void Finalize(this HarmonyMethod harmonyMethod, MethodInfo method)
            => method.PatchWith(finalizer: harmonyMethod);
    }
}