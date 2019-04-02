using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;

namespace Xpand.XAF.Modules.CloneMemberValue {
    public sealed partial class CloneMemberValueModule : ModuleBase {
        public CloneMemberValueModule() {
            InitializeComponent();
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect().Subscribe();
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelMember,IModelMemberCloneValue>();
            extenders.Add<IModelPropertyEditor, IModelCommonMemberViewItemCloneValue>();
            extenders.Add<IModelColumn, IModelCommonMemberViewItemCloneValue>();
        }
    }
}
