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
            => manager.WhereApplication().ToObservable(ImmediateScheduler.Instance)
		    .SelectMany(application => Observable.Defer(() => retriedExecution(application)).Retry(application));
		

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

	    public static IObservable<T> WhenGeneratingModelNodes<T>(this XafApplication application,Expression<Func<IModelApplication,T>> selector=null) where T : IEnumerable<IModelNode> 
		    => application.WhenApplicationModulesManager().SelectMany(manager => manager.WhenGeneratingModelNodes(selector));

	    public static IObservable<T> WhenGeneratingModelNodes<T>(this ApplicationModulesManager manager,Expression<Func<IModelApplication,T>> selector=null,bool emitCached=false) where T : IEnumerable<IModelNode> 
		    => manager.Modules.OfType<ReactiveModule>().First().GeneratingModelNodes
			    .SelectMany(updaters => {
				    var updaterType = (Type)typeof(T).GetCustomAttributesData().First(data => data.AttributeType==typeof(ModelNodesGeneratorAttribute)).ConstructorArguments.First().Value;
				    var updater = typeof(NodesUpdater<>).MakeGenericType(updaterType).CreateInstance();
				    updaters.Add((IModelNodesGeneratorUpdater) updater);
				    var name =emitCached? nameof(NodesUpdater<ModelNodesGeneratorBase>.UpdateCached):nameof(NodesUpdater<ModelNodesGeneratorBase>.Update);
				    return ((IObservable<ModelNode>) updater.GetPropertyValue(name)).Cast<T>();
			    });

	    class NodesUpdater<T> : ModelNodesGeneratorUpdater<T> where T : ModelNodesGeneratorBase{
		    readonly Subject<ModelNode> _updateSubject = new Subject<ModelNode>();
		    readonly Subject<ModelNode> _updateCachedSubject = new Subject<ModelNode>();

		    public IObservable<ModelNode> Update => _updateSubject.AsObservable();
		    public IObservable<ModelNode> UpdateCached => _updateCachedSubject.AsObservable();

		    public override void UpdateNode(ModelNode node) => _updateSubject.OnNext(node);

		    public override void UpdateCachedNode(ModelNode node) => _updateCachedSubject.OnNext(node);
	    }

    }
}