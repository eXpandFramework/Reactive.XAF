using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace DevExpress.XAF.Modules.ModelViewIneritance {
    public sealed partial class ModelViewIneritanceModule : ModuleBase {
        public ModelViewIneritanceModule() {
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
