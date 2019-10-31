using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using HarmonyLib;

namespace Xpand.XAF.Modules.CloneModelView{
    
    public sealed class CloneModelViewModule : ModuleBase{
        static CloneModelViewModule(){
            var harmony = new Harmony(typeof(IModelViewController).Namespace);
            harmony.PatchAll(typeof(CloneModelViewModule).Assembly);
        }


        public const string CategoryName = "Xpand.XAF.Modules.CloneModelView";


    }
}