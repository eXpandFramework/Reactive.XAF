using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Xpand.Extensions.XAF.Model;


namespace Xpand.XAF.Modules.Reactive.Logger{
    public interface IModelReactiveModuleLogger:IModelReactiveModule{
        IModelReactiveLogger ReactiveLogger{ get; }
    }

    public static class ModelReactiveModuleLogger{
        public static IObservable<IModelReactiveLogger> ReactiveLogger(this IObservable<IModelReactiveModules> source){
            return source.Select(modules => modules.ReactiveLogger());
        }
        public static IModelReactiveLogger ReactiveLogger(this IModelReactiveModules reactiveModules){
            return ((IModelReactiveModuleLogger) reactiveModules).ReactiveLogger;
        }
    }
    public interface IModelReactiveLogger:IModelNode{
        IModelTraceSourcedModules TraceSources{ get; }
    }

    public static class ModelReactiveLoggerService{
        public static IModelList<IModelTraceSourcedModule> GetActiveSources(this IModelReactiveLogger logger){
            return logger.TraceSources.Enabled
                ? (IModelList<IModelTraceSourcedModule>) logger.TraceSources
                : new CalculatedModelNodeList<IModelTraceSourcedModule>(Enumerable.Empty<IModelTraceSourcedModule>());
        }

    }
    [ModelNodesGenerator(typeof(TraceSourcedModulesNodesGenerator))]
    public interface IModelTraceSourcedModules:IModelNode,IModelList<IModelTraceSourcedModule>{
        bool Enabled{ get; set; }
    }

    public class TraceEventAppearenceRulesGenerator:ModelNodesGeneratorUpdater<AppearanceRulesModelNodesGenerator>{
        public static readonly Dictionary<Color, string> Modules=new Dictionary<Color, string>{
            {Color.Black, nameof(ReactiveModule)},
            {Color.DimGray, nameof(ReactiveLoggerModule)},
            {Color.DarkOrange, "ReactiveLoggerHubModule"},
            {Color.Blue, "AutoCommitModule"},
            {Color.BlueViolet, "CloneMemberValueModule"},
            {Color.Brown, "CloneModelViewModule"},
            {Color.BurlyWood, "GridListEditorModule"},
            {Color.CadetBlue, "HideToolBarModule"},
            {Color.Chartreuse, "MasterDetailModule"},
            {Color.Chocolate, "ModelMapperModule"},
            {Color.DarkGoldenrod, "ModelViewInheritanceModule"},
            {Color.DarkGray, "OneViewModule"},
            {Color.DarkGreen, "ProgressBarViewItemModule"},
            {Color.DarkKhaki, "RefreshViewModule"},
            {Color.DarkMagenta, "SuppressConfirmationModule"},
            {Color.DarkRed, "ViewEditModule"}
        };

        public override void UpdateNode(ModelNode node){
            if (node.GetParent<IModelClass>().TypeInfo.Type==typeof(TraceEvent)){
                foreach (var module in Modules){
                    var modelAppearanceRule = node.AddNode<IModelAppearanceRule>($"{module.Value}Source");
                    modelAppearanceRule.TargetItems = nameof(TraceEvent.Source);
                    modelAppearanceRule.FontColor=module.Key;
                    modelAppearanceRule.Context = "ListView";
                    modelAppearanceRule.Criteria = "[" + nameof(TraceEvent.Source) + "] = '" + module.Value + "'";
                }
            }
        }
    }
    public class TraceSourcedModulesNodesGenerator:ModelNodesGeneratorBase{
        protected override void GenerateNodesCore(ModelNode node){
            foreach (var module in TraceEventAppearenceRulesGenerator.Modules){
                node.AddNode<IModelTraceSourcedModule>(module.Value);
            }
            var modules = TraceEventAppearenceRulesGenerator.Modules
                .SelectMany(_ => ((IModelSources) node.Application).Modules.Where(m => m.Name==_.Value).ToTraceSource());
            foreach (var valueTuple in modules){
                var moduleName = valueTuple.module.Name;
                AddTraceSource(node, moduleName, valueTuple);
            }

        }

        private static void AddTraceSource(ModelNode node, string moduleName,
            (ModuleBase module, TraceSource traceSource) valueTuple){
            if (node[moduleName]==null){
                var sourcedModule = node.AddNode<IModelTraceSourcedModule>(moduleName);
                sourcedModule.Level = valueTuple.traceSource.Switch.Level;
            }
        }
    }

    public interface IModelTraceSourcedModule:IModelNode{
        [DefaultValue(SourceLevels.Verbose)]
        SourceLevels Level{ get; set; }
        
    }

    
    
}