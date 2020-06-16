using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using JetBrains.Annotations;

namespace Xpand.XAF.Modules.ModelViewInheritance {
    [UsedImplicitly]
    public sealed class ModelViewInheritanceModule : ModuleBase {
        public ModelViewInheritanceModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders) {
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelObjectView, IModelObjectViewMergedDifferences>();
        }

        public override void AddGeneratorUpdaters(ModelNodesGeneratorUpdaters updaters) {
            updaters.Add(new ModelViewInheritanceUpdater());
        }

    }
    
}
