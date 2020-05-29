using System.Diagnostics.CodeAnalysis;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Fasterflect;
using HarmonyLib;

namespace Xpand.XAF.Modules.Reactive{
    public abstract partial class ReactiveModuleBase{
        static ReactiveModuleBase(){
            var harmony = new Harmony(typeof(ReactiveModuleBase).FullName);
            var original = typeof(ApplicationModulesManager).Method("SetupModules");
            var prefix = typeof(ReactiveModule).Method(nameof(SetupModulesPatch),Flags.StaticAnyVisibility);
            harmony.Patch(original,  new HarmonyMethod(prefix));
        }
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static bool SetupModulesPatch(ApplicationModulesManager __instance){
            Tracing.Tracer.LogText("SetupModules");
            return SetupModules(__instance);

        }
    }
}