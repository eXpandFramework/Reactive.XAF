using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.SystemModule;

namespace Xpand.XAF.Modules.CloneModelView{
    public sealed class CloneModelViewModule : ModuleBase{
        public const string CategoryName = "Xpand.XAF.Modules.CloneModelView";

        public CloneModelViewModule(){
            RequiredModuleTypes.Add(typeof(SystemModule));
        }

        public override void AddGeneratorUpdaters(ModelNodesGeneratorUpdaters updaters){
            base.AddGeneratorUpdaters(updaters);
            updaters.Add(new ModelViewClonerUpdater());
        }
    }
}