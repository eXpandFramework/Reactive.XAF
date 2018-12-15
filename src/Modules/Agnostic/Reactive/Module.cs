using System;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.XAF.Modules.Reactive.Services;

namespace DevExpress.XAF.Modules.Reactive {
    public sealed partial class ReactiveModule : ModuleBase {
        readonly Subject<ITypesInfo> _typesInfoSubject=new Subject<ITypesInfo>();
        public IObservable<ITypesInfo> TypesInfo;
        public ReactiveModule() {
            TypesInfo = _typesInfoSubject;
            InitializeComponent();
        }


        public override void CustomizeTypesInfo(ITypesInfo typesInfo) {
            base.CustomizeTypesInfo(typesInfo);
            _typesInfoSubject.OnNext(typesInfo);
            _typesInfoSubject.OnCompleted();
        }

        public override void Setup(XafApplication application) {
            base.Setup(application);
            RxApp.XafApplication = application;
        }

    }
}
