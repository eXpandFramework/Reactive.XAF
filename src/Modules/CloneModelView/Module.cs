using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Source.Extensions.XAF;

namespace Xpand.XAF.Modules.CloneModelView{
    public sealed class CloneModelViewModule : XafModule{
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