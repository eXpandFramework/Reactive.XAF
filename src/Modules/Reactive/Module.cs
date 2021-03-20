using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using JetBrains.Annotations;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Reactive {
    public sealed class ReactiveModule : ReactiveModuleBase {
        [PublicAPI]
        public static ReactiveTraceSource TraceSource{ get; set; }
        readonly Subject<ModelInterfaceExtenders> _extendingModelSubject=new Subject<ModelInterfaceExtenders>();
        readonly Subject<ModelNodesGeneratorUpdaters> _generatingModelNodesSubject=new Subject<ModelNodesGeneratorUpdaters>();
        internal IObservable<ModelInterfaceExtenders> ExtendingModel=>_extendingModelSubject;
        internal Subject<ModelNodesGeneratorUpdaters> GeneratingModelNodes=>_generatingModelNodesSubject;

        static ReactiveModule() => TraceSource=new ReactiveTraceSource(nameof(ReactiveModule));

        public ReactiveModule() => RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            _extendingModelSubject.OnNext(extenders);
            extenders.Add<IModelApplication,IModelApplicationReactiveModules>();
        }

        public override void AddGeneratorUpdaters(ModelNodesGeneratorUpdaters updaters){
	        base.AddGeneratorUpdaters(updaters);
            _generatingModelNodesSubject.OnNext(updaters);
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .Subscribe(this);
        }
    }
}
