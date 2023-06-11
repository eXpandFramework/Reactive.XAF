using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Base.General;

using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive.Services;


namespace Xpand.XAF.Modules.Reactive.Logger{
    public interface IModelReactiveModuleLogger:IModelReactiveModule{
        IModelReactiveLogger ReactiveLogger{ get; }
    }

    public static class ModelReactiveModuleLogger{
        
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
        [Description("Requires XAF NotificationsModule")]
        IModelReactiveLoggerNotifications Notifications { get; }
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
        [DefaultValue(ObservableTraceStrategy.OnError)]
        ObservableTraceStrategy PersistStrategy{ get; set; }
        
        [CriteriaOptions(nameof(TraceEventType))]
        [Editor("DevExpress.ExpressApp.Win.Core.ModelEditor.CriteriaModelEditorControl, DevExpress.ExpressApp.Win" + XafAssemblyInfo.VersionSuffix + XafAssemblyInfo.AssemblyNamePostfix, DevExpress.Utils.ControlConstants.UITypeEditor)]
        string PersistStrategyCriteria { get; set; }
        [Browsable(false)]
        Type TraceEventType { get;  }
    }

    [DomainLogic(typeof(IModelTraceSourcedModules))]
    public class ModelTraceSourcedModulesLogic {
        public static Type Get_TraceEventType(IModelTraceSourcedModules modules) => typeof(TraceEvent);
    }
    public interface IModelReactiveLoggerNotifications:IModelList<IModelReactiveLoggerNotification>,IModelNode {
        
    }
    public interface IModelReactiveLoggerNotification : IModelNode {
        [CriteriaOptions(nameof(TraceEventObjectType))]
        [Editor("DevExpress.ExpressApp.Win.Core.ModelEditor.CriteriaModelEditorControl, DevExpress.ExpressApp.Win" + XafAssemblyInfo.VersionSuffix + XafAssemblyInfo.AssemblyNamePostfix, DevExpress.Utils.ControlConstants.UITypeEditor)]
        string Criteria { get; set; }
        [Required]
        [DataSourceProperty(nameof(Types))]
        IModelClass ObjectType { get; set; }
        [Browsable(false)]
        Type TraceEventObjectType { get;  }

        [Category("XafMessage")]
        bool ShowXafMessage { get; set; }
        
        IModelList<IModelClass> Types { get; }
        [ModelBrowsable(typeof(ShowXafMessageVisibility))][Category("XafMessage")]
        InformationType XafMessageType { get; set; }
        [ModelBrowsable(typeof(ShowXafMessageVisibility))]
        [DefaultValue(ShowMessageExtensions.MessageDisplayInterval)]
        [Category("XafMessage")]
        int MessageDisplayInterval { get; set; }
    }

    public class ShowXafMessageVisibility:IModelIsVisible {
        public bool IsVisible(IModelNode node, string propertyName) => ((IModelReactiveLoggerNotification)node).ShowXafMessage;
    }

    [DomainLogic(typeof(IModelReactiveLoggerNotification))]
    public class ModelReactiveLoggerNotificationLogic {
        public static Type Get_TraceEventObjectType(IModelReactiveLoggerNotification notification)
            => typeof(TraceEvent);
        
        public static IModelList<IModelClass> Get_Types(IModelReactiveLoggerNotification notification) 
            => notification.Application.BOModel.Where(c => typeof(ISupportNotifications).IsAssignableFrom(c.TypeInfo.Type)).ToCalculatedModelNodeList();
    }
    

    public class TraceEventAppearanceRulesGenerator:ModelNodesGeneratorUpdater<AppearanceRulesModelNodesGenerator>{
        private const string Reactive = "Reactive";
        private const string Data = "Data";
        private const string Azure = "Azure";
        private const string Network = "Network";
        private const string Editors = "Editors";
        private const string Model = "Model";
        private const string Office = "Office";
        private const string View = "View";
        public static readonly Dictionary<string, Color> Colors=new() {
            {Reactive,Color.Black},
            {Data,Color.DimGray},
            {Azure,Color.DimGray},
            {Network,Color.Olive},
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
            {"ObjectStateManagerModule",Colors[Data]},
            {"JobSchedulerNotificationModule",Colors[Data]},
            {"CloneMemberValueModule",Colors[Data]},
            {"RestModule",Colors[Data]},
            {"Notification",Colors[Data]},
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
            {"RazorViewModule",Colors[Office]},
            {"TenantManagerModule",Colors[Azure]},
            {"SpeechModule",Colors[Azure]},
            {"EmailModule",Colors[Office]},
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