using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.SuppressConfirmation{
    public sealed class SuppressConfirmationModule : ReactiveModuleBase{
        public const string CategoryName = "Xpand.XAF.Modules.SupressConfirmation";
        public static ReactiveTraceSource TraceSource{ get; set; }
        static SuppressConfirmationModule(){
            TraceSource=new ReactiveTraceSource(nameof(SuppressConfirmationModule));
        }
        public SuppressConfirmationModule(){
            RequiredModuleTypes.Add(typeof(SystemModule));
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
            extenders.Add<IModelClass, IModelClassSupressConfirmation>();
            extenders.Add<IModelObjectView, IModelObjectViewSupressConfirmation>();
            
        }

    }
}