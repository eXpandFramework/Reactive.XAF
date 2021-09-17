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
using Xpand.Extensions.XAF.ModelExtensions;


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
        public static IEnumerable<IModelTraceSourcedModule> GetEnabledSources(this IModelReactiveLogger logger){
            return logger.TraceSources.Enabled
                ? new CalculatedModelNodeList<IModelTraceSourcedModule>( logger.TraceSources.Where(_ => _.Level!=SourceLevels.Off))
                : new CalculatedModelNodeList<IModelTraceSourcedModule>(Enumerable.Empty<IModelTraceSourcedModule>());
        }

    }
    [ModelNodesGenerator(typeof(TraceSourcedModulesNodesGenerator))]
    public interface IModelTraceSourcedModules:IModelNode,IModelList<IModelTraceSourcedModule>{
        [DefaultValue(false)]
        bool Enabled{ get; set; }
        [DefaultValue(false)]
        bool Persist{ get; set; }
    }

    public class TraceEventAppearanceRulesGenerator:ModelNodesGeneratorUpdater<AppearanceRulesModelNodesGenerator>{
        private const string Reactive = "Reactive";
        private const string Data = "Data";
        private const string Editors = "Editors";
        private const string Model = "Model";
        private const string Office = "Office";
        private const string View = "View";
        public static readonly Dictionary<string, Color> Colors=new() {
            {Reactive,Color.Black},
            {Data,Color.DimGray},
            {Editors,Color.DarkOrange},
            {Model,Color.Blue},
            {Office,Color.BlueViolet},
            {View,Color.Brown},
        };

        public static readonly Dictionary<string, Color> Modules=new() {
            {nameof(ReactiveModule),Colors[Reactive]},
            {nameof(ReactiveLoggerModule),Colors[Reactive]},
            {"ReactiveLoggerHubModule",Colors[Reactive]},
            {"AutoCommitModule",Colors[Data]},
            {"JobSchedulerModule",Colors[Data]},
            {"CloneMemberValueModule",Colors[Data]},
            {"RestModule",Colors[Data]},
            {"CloneModelViewModule",Colors[Model]},
            {"ModelEditorWindowsFormsModule",Colors[Model]},
            {"GridListEditorModule",Colors[Editors]},
            {"ViewWizardModule",Colors[View]},
            {"ViewItemValueModule",Colors[View]},
            {"HideToolBarModule",Colors[View]},
            {"MasterDetailModule",Colors[View]},
            {"PositionInListViewModule",Colors[View]},
            {"ModelMapperModule",Colors[Model]},
            {"WindowsModule",Colors[Model]},
            {"ModelViewInheritanceModule",Colors[Model]},
            {"OneViewModule",Colors[View]},
            {"ProgressBarViewItemModule",Colors[Editors]},
            {"RefreshViewModule",Colors[View]},
            {"SuppressConfirmationModule",Colors[View]},
            {"LookupCascadeModule",Colors[Editors]},
            {"SequenceGeneratorModule",Colors[Data]},
            {"MicrosoftTodoModule",Colors[Office]},
            {"MicrosoftCalendarModule",Colors[Office]},
            {"MicrosoftModule",Colors[Office]},
            {"GoogleModule",Colors[Office]},
            {"GoogleTasksModule",Colors[Office]},
            {"GoogleCalendarModule",Colors[Office]},
            {"DocumentStyleManagerModule",Colors[Office]},
            {"ViewEditModule",Colors[View]}
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
            foreach (var module in TraceEventAppearanceRulesGenerator.Modules){
                node.AddNode<IModelTraceSourcedModule>(module.Key);
            }
            var modules = TraceEventAppearanceRulesGenerator.Modules
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