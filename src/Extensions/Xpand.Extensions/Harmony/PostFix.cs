using System.Reflection;
using HarmonyLib;

namespace Xpand.Extensions.Harmony {
    public static partial class HarmonyExtensions {
        public static void PostFix(this HarmonyMethod harmonyMethod, MethodInfo method)
            => method.PatchWith(postFix: harmonyMethod);
    }
}