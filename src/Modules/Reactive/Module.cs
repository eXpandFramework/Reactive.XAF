using System;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive {
    public sealed class ReactiveModule : ReactiveModuleBase {
        public static ReactiveTraceSource TraceSource{ get; set; }
        readonly Subject<ITypesInfo> _typesInfoSubject=new Subject<ITypesInfo>();
        
        readonly Subject<ModelInterfaceExtenders> _extendModelSubject=new Subject<ModelInterfaceExtenders>();

        public IObservable<ITypesInfo> ModifyTypesInfo => _typesInfoSubject;

        public IObservable<ModelInterfaceExtenders> ExtendModel=>_extendModelSubject;

        static ReactiveModule(){
            
            TraceSource=new ReactiveTraceSource(nameof(ReactiveModule));

        }
        public ReactiveModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            _extendModelSubject.OnNext(extenders);
            extenders.Add<IModelApplication,IModelApplicationReactiveModules>();
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .TakeUntilDisposed(this)
                .Subscribe();
        }

        public override void CustomizeTypesInfo(ITypesInfo typesInfo) {
            base.CustomizeTypesInfo(typesInfo);
            _typesInfoSubject.OnNext(typesInfo);
        }


    }
}
