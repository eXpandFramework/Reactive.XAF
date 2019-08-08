using System;
using System.Collections.ObjectModel;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Fasterflect;
using HarmonyLib;

namespace Xpand.XAF.Modules.Reactive{
    public abstract partial class ReactiveModuleBase{
        static ReactiveModuleBase(){
            var harmony = new Harmony(typeof(ReactiveModuleBase).FullName);
            var original = typeof(ApplicationModulesManager).Method("SetupModules");
            var prefix = typeof(ReactiveModule).Method(nameof(SetupModules),Flags.StaticAnyVisibility);
            harmony.Patch(original,  new HarmonyMethod(prefix));
        }

        static bool SetupModules(ApplicationModulesManager __instance){
            Tracing.Tracer.LogText("SetupModules");
            foreach(var module in __instance.Modules) {
                try {
                    module.Setup(__instance);
                    if (module is ReactiveModuleBase reactiveModuleBase){
                        reactiveModuleBase.SetupCompletedSubject.OnNext(reactiveModuleBase);
                        reactiveModuleBase.SetupCompletedSubject.OnCompleted();
                    }
                }
                catch(Exception e) {
                    throw new InvalidOperationException($"Exception occurs while initializing the '{module.GetType().FullName}' module: {e.Message}", e);
                }
            }
            foreach(var controller in ((ReadOnlyCollection<Controller>) __instance.ControllersManager.GetPropertyValue("Controllers"))) {
                if(controller is ISupportSetup supportSetupItem) {
                    try {
                        supportSetupItem.Setup(__instance);
                    }
                    catch(Exception e) {
                        throw new InvalidOperationException($"Exception occurs while initializing the '{controller.GetType().FullName}' controller: {e.Message}", e);
                    }
                }
            }            
            return false;
            
        }
    }
}