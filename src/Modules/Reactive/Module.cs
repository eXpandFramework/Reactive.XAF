using System;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using JetBrains.Annotations;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive {
    public sealed class ReactiveModule : ReactiveModuleBase {
        [PublicAPI]
        public static ReactiveTraceSource TraceSource{ get; set; }
        readonly Subject<ModelInterfaceExtenders> _extendingModelSubject=new();
        static readonly Subject<ModelNodesGeneratorUpdaters> GeneratingModelNodesSubject=new();
        internal IObservable<ModelInterfaceExtenders> ExtendingModel=>_extendingModelSubject;
        public static Subject<ModelNodesGeneratorUpdaters> GeneratingModelNodes=>GeneratingModelNodesSubject;

        static ReactiveModule() => TraceSource=new ReactiveTraceSource(nameof(ReactiveModule));

        public ReactiveModule() => RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            _extendingModelSubject.OnNext(extenders);
            extenders.Add<IModelApplication,IModelApplicationReactiveModules>();
            extenders.Add<IModelReactiveModules,IModelReactiveModule>();
        }

        public override void AddGeneratorUpdaters(ModelNodesGeneratorUpdaters updaters){
	        base.AddGeneratorUpdaters(updaters);
            GeneratingModelNodesSubject.OnNext(updaters);
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .Subscribe(this);
        }
    }
}
