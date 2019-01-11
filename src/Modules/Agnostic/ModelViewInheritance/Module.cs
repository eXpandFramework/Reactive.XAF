using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Xpand.XAF.Modules.ModelViewInheritance;

namespace DevExpress.XAF.Modules.ModelViewInheritance {
    public sealed partial class ModelViewInheritanceModule : ModuleBase {
        public ModelViewInheritanceModule() {
            InitializeComponent();
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
