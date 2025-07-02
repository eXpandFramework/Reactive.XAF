using System;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Reactive {
    public sealed class ReactiveModule : ReactiveModuleBase {
        
        public static ReactiveTraceSource TraceSource{ get; set; }
        readonly Subject<ModelInterfaceExtenders> _extendingModelSubject=new();
        
        internal IObservable<ModelInterfaceExtenders> ExtendingModel=>_extendingModelSubject;
        

        static ReactiveModule() => TraceSource=new ReactiveTraceSource(nameof(ReactiveModule));

        public ReactiveModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            _extendingModelSubject.OnNext(extenders);
            extenders.Add<IModelApplication,IModelApplicationReactiveModules>();
            extenders.Add<IModelReactiveModules,IModelReactiveModule>();
            extenders.Add<IModelAppearanceRule, IModelAppearanceWithToolTipRule>();
        }

        public IObservable<ModelNodesGeneratorUpdaters> WhenGeneratorUpdaters() => _generatorUpdaterSubject;

        private readonly Subject<ModelNodesGeneratorUpdaters> _generatorUpdaterSubject = new();

        public override void AddGeneratorUpdaters(ModelNodesGeneratorUpdaters updaters){
	        base.AddGeneratorUpdaters(updaters);
            _generatorUpdaterSubject.OnNext(updaters);
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .Subscribe(this);
        }

        
    }
}
