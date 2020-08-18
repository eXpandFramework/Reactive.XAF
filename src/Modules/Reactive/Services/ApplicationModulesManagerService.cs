using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Fasterflect;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ApplicationModulesManagerExtensions;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ApplicationModulesManagerService{
	    public static IObservable<T> WhenApplication<T>(this ApplicationModulesManager manager,Func<XafApplication,IObservable<T>> retriedExecution) 
            => manager.WhereApplication().ToObservable()
		    .SelectMany(application => retriedExecution(application).Retry(application));
		

	    public static IObservable<(ApplicationModulesManager manager, CustomizeTypesInfoEventArgs e)> WhenCustomizeTypesInfo(this IObservable<ApplicationModulesManager> source) 
            => source.SelectMany(manager => manager.WhenCustomizeTypesInfo());

	    public static IObservable<(ApplicationModulesManager manager, CustomizeTypesInfoEventArgs e)> WhenCustomizeTypesInfo(this ApplicationModulesManager manager) 
            => Observable.FromEventPattern<EventHandler<CustomizeTypesInfoEventArgs>, CustomizeTypesInfoEventArgs>(
			        h => manager.CustomizeTypesInfo += h, h => manager.CustomizeTypesInfo += h, ImmediateScheduler.Instance)
		        .TransformPattern<CustomizeTypesInfoEventArgs,ApplicationModulesManager>();
	    
	    public static IObservable<ModelInterfaceExtenders> WhenExtendingModel(this IObservable<ApplicationModulesManager> source) 
            => source.SelectMany(manager => manager.WhenExtendingModel());

	    public static IObservable<ModelInterfaceExtenders> WhenExtendingModel(this ApplicationModulesManager manager) =>
	        manager.Modules.OfType<ReactiveModule>().First().ExtendingModel;

        public static IObservable<T> WhenGeneratingModelNodes<T>(this IObservable<ApplicationModulesManager> source,Expression<Func<IModelApplication,T>> selector=null) where T : IEnumerable<IModelNode> 
            => source.SelectMany(manager => manager.WhenGeneratingModelNodes(selector));

	    public static IObservable<T> WhenGeneratingModelNodes<T>(this ApplicationModulesManager manager,Expression<Func<IModelApplication,T>> selector=null) where T : IEnumerable<IModelNode> =>
		    manager.Modules.OfType<ReactiveModule>().First().GeneratingModelNodes
			    .SelectMany(updaters => {
				    var updaterType = (Type)typeof(T).GetCustomAttributesData().First(data => data.AttributeType==typeof(ModelNodesGeneratorAttribute)).ConstructorArguments.First().Value;
				    var updater = typeof(NodesUpdater<>).MakeGenericType(updaterType).CreateInstance();
				    updaters.Add((IModelNodesGeneratorUpdater) updater);
				    return ((IObservable<ModelNode>) updater.GetPropertyValue(nameof(NodesUpdater<ModelNodesGeneratorBase>.Nodes))).Cast<T>();
			    });
	    
	    class NodesUpdater<T> : ModelNodesGeneratorUpdater<T> where T : ModelNodesGeneratorBase{
		    readonly Subject<ModelNode> _nodeSubject=new Subject<ModelNode>();

			public IObservable<ModelNode> Nodes => _nodeSubject.AsObservable();

			public override void UpdateNode(ModelNode node){
				_nodeSubject.OnNext(node);
		    }
	    }

    }
}