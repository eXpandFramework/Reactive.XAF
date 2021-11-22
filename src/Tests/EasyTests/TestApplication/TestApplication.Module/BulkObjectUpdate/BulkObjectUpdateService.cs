using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Fasterflect;
using TestApplication.Module.Common;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.BulkObjectUpdate;
using Xpand.XAF.Modules.Reactive;

namespace TestApplication.Module.BulkObjectUpdate {
    public static class BulkObjectUpdateService {
        public static IObservable<Unit> ConnectBulkObjectUpdate(this ApplicationModulesManager manager) {
            return manager.WhenGeneratingModelNodes1<IModelBulkObjectUpdateRules>(false, rules => {
                var rule = rules.AddNode<IModelBulkObjectUpdateRule>();
                rule.DetailView = (IModelDetailView)rules.Application.Views[TestTask.TaskBulkUpdateDetailView];
                rule.ListView = rules.Application.BOModel.GetClass(typeof(TestTask)).DefaultListView;
                rule.Caption = "Update";  
                }).ToUnit();
        }

        
        public static IObservable<T> WhenGeneratingModelNodes1<T>(this ApplicationModulesManager manager,bool emitCached,Action<T> update) where T : IEnumerable<IModelNode>,IModelNode 
            => ReactiveModule.GeneratingModelNodes
                .SelectMany(updaters => {
                    NodesUpdater<ModelNodesGeneratorBase>.AddAction(typeof(T),node => update((T)node),emitCached);
                    var updaterType = (Type)typeof(T).GetCustomAttributesData()
                        .First(data => data.AttributeType == typeof(ModelNodesGeneratorAttribute)).ConstructorArguments.First().Value;
                    var type = typeof(NodesUpdater<>).MakeGenericType(updaterType!);
                    var updater = type.CreateInstance(typeof(T));
                    updaters.Add((IModelNodesGeneratorUpdater) updater);
                    return Observable.Empty<T>();
                }).Finally(() => {});

    }

    class NodesUpdater<T> : ModelNodesGeneratorUpdater<T> where T : ModelNodesGeneratorBase{
        static readonly ConcurrentDictionary<Type, List<Action<IModelNode>>> Actions = new();
        static readonly ConcurrentDictionary<Type, List<Action<IModelNode>>> CachedActions = new();
        private readonly Type _interfaceType;

        public NodesUpdater(Type interfaceType) => _interfaceType = interfaceType;
        
        public override void UpdateNode(ModelNode node) => UpdateNode(node, NodesUpdater<ModelNodesGeneratorBase>.Actions);
        private void UpdateNode(ModelNode node, ConcurrentDictionary<Type, List<Action<IModelNode>>> actions) {
            actions.TryGetValue(_interfaceType, out var list);
            list?.ForEach(action => action(node));
        }

        public override void UpdateCachedNode(ModelNode node) => UpdateNode(node, NodesUpdater<ModelNodesGeneratorBase>.CachedActions);

        public static void AddAction(Type interfaceType,Action<IModelNode> action,bool emitCached) {
            GetActions(emitCached).AddOrUpdate(interfaceType, type => new List<Action<IModelNode>>() { action }, (type, list) => {
                list.Add(action);
                return list;
            });
        }

        static ConcurrentDictionary<Type, List<Action<IModelNode>>> GetActions(bool emitCached) 
            => emitCached ? CachedActions : Actions;
    }

}