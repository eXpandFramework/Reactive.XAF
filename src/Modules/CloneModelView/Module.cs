using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using HarmonyLib;
using JetBrains.Annotations;

namespace Xpand.XAF.Modules.CloneModelView{
    
    public sealed class CloneModelViewModule : ModuleBase{
        static CloneModelViewModule(){
            var harmony = new Harmony(typeof(IModelViewController).Namespace);
            harmony.PatchAll(typeof(CloneModelViewModule).Assembly);
        }

        [PublicAPI]
        public const string CategoryName = "Xpand.XAF.Modules.CloneModelView";


    }
}