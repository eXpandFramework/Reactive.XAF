using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.GridListEditor {
    public sealed class GridListEditorModule : ReactiveModuleBase{
        public const string CategoryName = "Xpand.XAF.Modules.GridListEditor";
        public static ReactiveTraceSource TraceSource{ get; set; }

        static GridListEditorModule(){
            TraceSource=new ReactiveTraceSource(nameof(GridListEditorModule));
        }
        public GridListEditorModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
            
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            Application.Connect()
                .TakeUntilDisposed(this)
                .Subscribe();
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveModules,IModelReactiveModuleGridListEditor>();
        }
    }
}
