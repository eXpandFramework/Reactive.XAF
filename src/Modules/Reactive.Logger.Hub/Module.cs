using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Win;
using Xpand.XAF.Modules.GridListEditor;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Win;

namespace Xpand.XAF.Modules.Reactive.Logger.Hub {
    public sealed class ReactiveLoggerHubModule : ReactiveModuleBase{
        public const string CategoryName = "Xpand.XAF.Modules.Reactive.Logger.Hub";

        static ReactiveLoggerHubModule(){
            TraceSource=new ReactiveTraceSource(nameof(ReactiveLoggerHubModule));
            TraceExtensions.DefaultMembers.TryGetValue(typeof(Window), out var func);
            TraceExtensions.DefaultMembers.TryAdd(typeof(WinWindow), func);
        }
        public ReactiveLoggerHubModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModuleWin));
            RequiredModuleTypes.Add(typeof(GridListEditorModule));
            RequiredModuleTypes.Add(typeof(ReactiveLoggerModule));
        }

        public static ReactiveTraceSource TraceSource{ get; set; }
        public override void AddGeneratorUpdaters(ModelNodesGeneratorUpdaters updaters){
            base.AddGeneratorUpdaters(updaters);
            updaters.Add(new TopIndexRuleModelUpdater());
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            Application?.Connect()
                .TakeUntil(Application.WhenDisposed())
                .Subscribe();
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveLogger,IModelServerPorts>();
            
        }
    }

    class TopIndexRuleModelUpdater:ModelNodesGeneratorUpdater<ModelGridListEditorRulesNodesGenerator>{
        public override void UpdateNode(ModelNode node){
            var rule = ((IModelGridListEditorRules) node).AddNode<IModelGridListEditorTopRow>("TraceEvent ListView");
            rule.ListView = node.Application.BOModel.GetClass(typeof(TraceEvent)).DefaultListView;
        }
    }
}
