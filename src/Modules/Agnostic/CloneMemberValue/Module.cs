using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.CloneMemberValue {
    public sealed partial class CloneMemberValueModule : ModuleBase {
        public CloneMemberValueModule() {
            InitializeComponent();
        }

        public override void Setup(XafApplication application){
            base.Setup(application);
            CloneMemberValueService.CloneMemberValues
                .Tracer(true)
                .TakeUntilDisposingMainWindow()
                .Subscribe();
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelMember,IModelMemberCloneValue>();
            extenders.Add<IModelPropertyEditor, IModelCommonMemberViewItemCloneValue>();
            extenders.Add<IModelColumn, IModelCommonMemberViewItemCloneValue>();
        }
    }
}
