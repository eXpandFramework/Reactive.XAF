using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.HideToolBar{
    public sealed class HideToolBarModule : ReactiveModuleBase{
        public const string CategoryName = "Xpand.XAF.Modules.HideToolBar";

        public HideToolBarModule(){
            RequiredModuleTypes.Add(typeof(SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));   
        }

        public static ReactiveTraceSource TraceSource{ get; set; }
        static HideToolBarModule(){
            TraceSource=new ReactiveTraceSource(nameof(HideToolBarModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .TakeUntilDisposed(this)
                .Subscribe();
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelClass, IModelClassHideToolBar>();
            extenders.Add<IModelListView, IModelListViewHideToolBar>();
        }

    }
}