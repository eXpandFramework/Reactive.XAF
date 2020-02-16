using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.MasterDetail{
    [UsedImplicitly]
    public sealed class MasterDetailModule : ReactiveModuleBase{
        public const string CategoryName = "Xpand.XAF.Modules.MasterDetail";
        [PublicAPI]
        public static ReactiveTraceSource TraceSource{ get; set; }
        static MasterDetailModule(){
            TraceSource=new ReactiveTraceSource(nameof(MasterDetailModule));
        }
        
        public MasterDetailModule(){
            RequiredModuleTypes.Add(typeof(SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect(Application)
                .TakeUntilDisposed(this)
                .Subscribe();
        }
        
        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelDashboardView, IModelDashboardViewMasterDetail>();
            extenders.Add<IModelApplication, IModelApplicationMasterDetail>();
        }
    }
}