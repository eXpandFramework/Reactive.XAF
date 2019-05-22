using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using Xpand.Source.Extensions.XAF;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive {
    public sealed class ReactiveModule : XafModule {
        readonly Subject<ITypesInfo> _typesInfoSubject=new Subject<ITypesInfo>();
        public IObservable<ITypesInfo> TypesInfo;

        public ReactiveModule() {
            TypesInfo = _typesInfoSubject;
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            Application?.Connect()
                .TakeUntil(this.WhenDisposed())
                .Subscribe();
        }

        public override void CustomizeTypesInfo(ITypesInfo typesInfo) {
            base.CustomizeTypesInfo(typesInfo);
            _typesInfoSubject.OnNext(typesInfo);
            _typesInfoSubject.OnCompleted();
        }


    }
}
