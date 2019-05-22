using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Source.Extensions.XAF;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.SuppressConfirmation{
    public sealed class SuppressConfirmationModule : XafModule{
        public const string CategoryName = "Xpand.XAF.Modules.SupressConfirmation";

        public SuppressConfirmationModule(){
            RequiredModuleTypes.Add(typeof(SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            Application.Connect()
                .TakeUntil(this.WhenDisposed())
                .Subscribe();
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelClass, IModelClassSupressConfirmation>();
            extenders.Add<IModelObjectView, IModelObjectViewSupressConfirmation>();
            
        }

    }
}