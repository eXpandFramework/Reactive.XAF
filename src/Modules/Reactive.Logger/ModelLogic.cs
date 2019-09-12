using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Utils.Extensions;

namespace Xpand.XAF.Modules.Reactive.Logger{
    public interface IModelReactiveModuleLogger:IModelReactiveModule{
        IModelReactiveLogger ReactiveLogger{ get; }
    }

    public static class ModelReactiveModuleLogger{
        public static IObservable<IModelReactiveLogger> ReactiveLogger(this IObservable<IModelReactiveModules> source){
            return source.Select(modules => modules.ReactiveLogger());
        }
        public static IModelReactiveLogger ReactiveLogger(this IModelReactiveModules reactiveModules){
            return reactiveModules.CastTo<IModelReactiveModuleLogger>().ReactiveLogger;
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

    public class TraceSourcedModulesNodesGenerator:ModelNodesGeneratorBase{
        protected override void GenerateNodesCore(ModelNode node){
            var modules = ((IModelSources) node.Application).Modules.ToTraceSource();
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