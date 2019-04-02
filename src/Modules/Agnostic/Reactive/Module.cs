using System;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Reactive {
    public sealed class ReactiveModule : ModuleBase {
        readonly Subject<ITypesInfo> _typesInfoSubject=new Subject<ITypesInfo>();
        public IObservable<ITypesInfo> TypesInfo;

        public ReactiveModule() {
            TypesInfo = _typesInfoSubject;
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
        }

        public override void CustomizeTypesInfo(ITypesInfo typesInfo) {
            base.CustomizeTypesInfo(typesInfo);
            _typesInfoSubject.OnNext(typesInfo);
            _typesInfoSubject.OnCompleted();
        }

        public override void Setup(XafApplication application) {
            base.Setup(application);
            application.Connect()
                .TakeUntilDisposingMainWindow()
                .Subscribe();
        }

    }
}
