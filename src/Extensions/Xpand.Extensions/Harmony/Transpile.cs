using System.Reflection;
using HarmonyLib;

namespace Xpand.Extensions.Harmony {
    public static partial class HarmonyExtensions {
        public static void Transpile(this HarmonyMethod harmonyMethod, MethodInfo method)
            => method.PatchWith(transpiler: harmonyMethod);
    }
}