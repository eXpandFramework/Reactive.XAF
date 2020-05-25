using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.CloneMemberValue{
    public sealed class CloneMemberValueModule : ReactiveModuleBase{
        public CloneMemberValueModule(){
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }

        [PublicAPI]
        public static ReactiveTraceSource TraceSource{ get; set; }
        static CloneMemberValueModule(){
            TraceSource=new ReactiveTraceSource(nameof(CloneMemberValueModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .TakeUntilDisposed(this)
                .Subscribe();
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelMember, IModelMemberCloneValue>();
            extenders.Add<IModelPropertyEditor, IModelCommonMemberViewItemCloneValue>();
            extenders.Add<IModelColumn, IModelCommonMemberViewItemCloneValue>();
        }
    }
}