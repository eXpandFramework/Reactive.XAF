using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Validation;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Email{
    [UsedImplicitly]
    public sealed class EmailModule : ReactiveModuleBase{
        
        [PublicAPI]
        public static ReactiveTraceSource TraceSource{ get; set; }
        static EmailModule(){
            TraceSource=new ReactiveTraceSource(nameof(EmailModule));
        }
        
        public EmailModule(){
            RequiredModuleTypes.Add(typeof(SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
            RequiredModuleTypes.Add(typeof(ValidationModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .TakeUntilDisposed(this)
                .Subscribe();
        }
        
        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveModules,IModelReactiveModulesEmail>();
        }
    }
}