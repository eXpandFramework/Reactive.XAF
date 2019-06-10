using System;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive {
    public sealed class ReactiveModule : ReactiveModuleBase {
        static readonly Subject<ITypesInfo> TypesInfoSubject=new Subject<ITypesInfo>();
        static readonly Subject<ModelInterfaceExtenders> ExtendModelSubject=new Subject<ModelInterfaceExtenders>();
        public IObservable<ITypesInfo> ModifyTypesInfo{ get; }=TypesInfoSubject;
        public IObservable<ModelInterfaceExtenders> ExtendModel{ get; }=ExtendModelSubject;

        public ReactiveModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            ExtendModelSubject.OnNext(extenders);
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .TakeUntilDisposed(this)
                .Subscribe();
        }

        public override void CustomizeTypesInfo(ITypesInfo typesInfo) {
            base.CustomizeTypesInfo(typesInfo);
            TypesInfoSubject.OnNext(typesInfo);
        }


    }
}
