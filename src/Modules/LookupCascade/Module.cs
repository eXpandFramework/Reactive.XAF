using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.LookupCascade {
    [UsedImplicitly]
    public sealed class LookupCascadeModule : ReactiveModuleBase{
        static LookupCascadeModule(){
            TraceSource=new ReactiveTraceSource(nameof(LookupCascade));
        }
        public LookupCascadeModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Web.SystemModule.SystemAspNetModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }

        public static ReactiveTraceSource TraceSource{ get; set; }

        public override void Setup(XafApplication application){
            base.Setup(application);
            
            this.Connect()
                .TakeUntilDisposed(this)
                .Subscribe();
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelOptions,IModelOptionsClientDatasource>();
            extenders.Add<IModelMemberViewItem,IModelMemberViewItemLookupCascadePropertyEditor>();
            extenders.Add<IModelColumn,IModelColumnClientVisible>();
            // extenders.Add<IModelReactiveModules,IModelReactiveModuleLogger>();
            
        }
    }

}
