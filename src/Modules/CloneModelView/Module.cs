using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.XAF.Modules.CloneModelView{
    public sealed class CloneModelViewModule : ModuleBase{
        public const string CategoryName = "Xpand.XAF.Modules.CloneModelView";


        public override void AddGeneratorUpdaters(ModelNodesGeneratorUpdaters updaters){
            base.AddGeneratorUpdaters(updaters);
            updaters.Add(new ModelViewClonerUpdater());
        }
    }
}