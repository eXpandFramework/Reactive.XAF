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
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.XAF.Model;


namespace Xpand.XAF.Modules.Reactive.Logger{
    public interface IModelReactiveModuleLogger:IModelReactiveModule{
        IModelReactiveLogger ReactiveLogger{ get; }
    }

    public static class ModelReactiveModuleLogger{
        [PublicAPI]
        public static IObservable<IModelReactiveLogger> ReactiveLogger(this IObservable<IModelReactiveModules> source){
            return source.Select(modules => modules.ReactiveLogger()).WhenNotDefault();
        }
        public static IModelReactiveLogger ReactiveLogger(this IModelReactiveModules reactiveModules){
            var modelReactiveModuleLogger = reactiveModules as IModelReactiveModuleLogger;
            return modelReactiveModuleLogger?.ReactiveLogger;
        }
    }
    public interface IModelReactiveLogger:IModelNode{
        IModelTraceSourcedModules TraceSources{ get; }
    }

    public static class ModelReactiveLoggerService{
        public static IEnumerable<IModelTraceSourcedModule> GetActiveSources(this IModelReactiveLogger logger){
            return logger.TraceSources.Enabled
                ? new CalculatedModelNodeList<IModelTraceSourcedModule>( logger.TraceSources.Where(_ => _.Level!=SourceLevels.Off))
                : new CalculatedModelNodeList<IModelTraceSourcedModule>(Enumerable.Empty<IModelTraceSourcedModule>());
        }

    }
    [ModelNodesGenerator(typeof(TraceSourcedModulesNodesGenerator))]
    public interface IModelTraceSourcedModules:IModelNode,IModelList<IModelTraceSourcedModule>{
        [DefaultValue(false)]
        bool Enabled{ get; set; }
    }

    public class TraceEventAppearenceRulesGenerator:ModelNodesGeneratorUpdater<AppearanceRulesModelNodesGenerator>{
        public static readonly Dictionary<string, Color> Modules=new Dictionary<string,Color>{
            {nameof(ReactiveModule),Color.Black},
            {nameof(ReactiveLoggerModule),Color.DimGray},
            {"ReactiveLoggerHubModule",Color.DarkOrange},
            {"AutoCommitModule",Color.Blue},
            {"CloneMemberValueModule",Color.BlueViolet},
            {"CloneModelViewModule",Color.Brown},
            {"GridListEditorModule",Color.BurlyWood},
            {"HideToolBarModule",Color.CadetBlue},
            {"MasterDetailModule",Color.Chartreuse},
            {"ModelMapperModule",Color.Chocolate},
            {"ModelViewInheritanceModule",Color.DarkGoldenrod},
            {"OneViewModule",Color.DarkGray},
            {"ProgressBarViewItemModule",Color.DarkGreen},
            {"RefreshViewModule",Color.DarkKhaki},
            {"SuppressConfirmationModule",Color.DarkMagenta},
            {"LookupCascadeModule",Color.Chocolate},
            {"SequenceGeneratorModule",Color.Firebrick},
            {"ViewEditModule",Color.DarkRed}
        };

        public override void UpdateNode(ModelNode node){
            if (node.GetParent<IModelClass>().TypeInfo.Type==typeof(TraceEvent)){
                foreach (var module in Modules){
                    var modelAppearanceRule = node.AddNode<IModelAppearanceRule>($"{module.Key}Source");
                    modelAppearanceRule.TargetItems = nameof(TraceEvent.Source);
                    modelAppearanceRule.FontColor=module.Value;
                    modelAppearanceRule.Context = "ListView";
                    modelAppearanceRule.Criteria = "[" + nameof(TraceEvent.Source) + "] = '" + module.Key + "'";
                }
            }
        }
    }
    public class TraceSourcedModulesNodesGenerator:ModelNodesGeneratorBase{
        protected override void GenerateNodesCore(ModelNode node){
            foreach (var module in TraceEventAppearenceRulesGenerator.Modules){
                node.AddNode<IModelTraceSourcedModule>(module.Key);
            }
            var modules = TraceEventAppearenceRulesGenerator.Modules
                .SelectMany(_ => ((IModelSources) node.Application).Modules.Where(m => m.Name==_.Key).ToTraceSource());
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