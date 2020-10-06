using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using HarmonyLib;
using JetBrains.Annotations;
using Xpand.Extensions.XAF.TypesInfoExtensions;

namespace Xpand.XAF.Modules.CloneModelView{
    
    public sealed class CloneModelViewModule : ModuleBase{
        static CloneModelViewModule(){
            var harmony = new Harmony(typeof(IModelViewController).Namespace);
            harmony.PatchAll(typeof(CloneModelViewModule).Assembly);
            XafTypesInfo.Instance.ReferenceNetStandard();
        }

        [PublicAPI]
        public const string CategoryName = "Xpand.XAF.Modules.CloneModelView";


    }
}