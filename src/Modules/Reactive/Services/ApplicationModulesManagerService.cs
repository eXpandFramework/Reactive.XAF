using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Fasterflect;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.ApplicationModulesManagerExtensions;
using Xpand.Extensions.XAF.Attributes.Custom;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ApplicationModulesManagerService {
        public static IObservable<(IMemberInfo memberInfo, T attribute)> AddTypesInfoAttribute<T>(this IObservable<ApplicationModulesManager> source) where T : Attribute
            => source.SelectMany(manager => manager.AddTypesInfoAttribute<T>());
        
        public static IObservable<(IMemberInfo memberInfo, T attribute)> AddTypesInfoAttribute<T>(this ApplicationModulesManager manager) where T : Attribute 
            => manager.WhenCustomizeTypesInfo()
                .SelectMany(t => t.e.TypesInfo.PersistentTypes.ToObservable(Scheduler.Immediate)
                    .SelectMany(info => info.Members.ToObservable(Scheduler.Immediate)
                        .Where(memberInfo => memberInfo.MemberType.RealType().IsNumeric(true))
                        .SelectMany(memberInfo => memberInfo.FindAttributes<T>().ToArray().ToObservable(Scheduler.Immediate)
                            .SwitchIfEmpty(typeof(T).CreateInstance().ReturnObservable().Cast<T>())
                            .Select(attribute=>(memberInfo,attribute)))
                        .Do(t1 => {
                            if (t1.attribute is ICustomAttribute customAttribute) {
                                t1.memberInfo.AddCustomAttribute(customAttribute);
                            }
                            else {
                                t1.memberInfo.AddAttribute(t1.attribute);
                            }
                        })));
        
        public static IObservable<T> WhenApplication<T>(this ApplicationModulesManager manager,Func<XafApplication,IObservable<T>> retriedExecution,bool emitInternalApplications=true) 
            => manager.WhereApplication().Where(application => emitInternalApplications||!application.IsInternal()).ToObservable(ImmediateScheduler.Instance)
	            .SelectMany(application => Observable.Defer(() => retriedExecution(application)).Retry(application));
		

	    public static IObservable<(ApplicationModulesManager manager, CustomizeTypesInfoEventArgs e)> WhenCustomizeTypesInfo(this IObservable<ApplicationModulesManager> source) 
            => source.SelectMany(manager => manager.WhenCustomizeTypesInfo());

	    public static IObservable<(ApplicationModulesManager manager, CustomizeTypesInfoEventArgs e)> WhenCustomizeTypesInfo(this ApplicationModulesManager manager) 
            => Observable.FromEventPattern<EventHandler<CustomizeTypesInfoEventArgs>, CustomizeTypesInfoEventArgs>(
			        h => manager.CustomizeTypesInfo += h, h => manager.CustomizeTypesInfo += h, ImmediateScheduler.Instance)
		        .TransformPattern<CustomizeTypesInfoEventArgs,ApplicationModulesManager>();
	    
	    public static IObservable<ModelInterfaceExtenders> WhenExtendingModel(this IObservable<ApplicationModulesManager> source) 
            => source.SelectMany(manager => manager.WhenExtendingModel());

	    public static IObservable<ModelInterfaceExtenders> WhenExtendingModel(this ApplicationModulesManager manager) 
            => manager.Modules.FindModule<ReactiveModule>().ExtendingModel;

        public static IObservable<T> WhenGeneratingModelNodes<T>(this IObservable<ApplicationModulesManager> source,Expression<Func<IModelApplication,T>> selector=null) where T : IEnumerable<IModelNode> 
            => source.SelectMany(manager => manager.WhenGeneratingModelNodes(selector));

	    public static IObservable<T> WhenGeneratingModelNodes<T>(this XafApplication application,Expression<Func<IModelApplication,T>> selector=null) where T : IEnumerable<IModelNode> 
		    => application.WhenApplicationModulesManager().SelectMany(manager => manager.WhenGeneratingModelNodes(selector));
	    
	    public static IObservable<T> WhenGeneratingModelNodes<T>(this ApplicationModulesManager manager,bool emitCached) where T : IEnumerable<IModelNode> 
		    => ReactiveModule.GeneratingModelNodes.WhenNotDefault()
			    .SelectMany(updaters => {
				    var updaterType = (Type)typeof(T).GetCustomAttributesData().First(data => data.AttributeType==typeof(ModelNodesGeneratorAttribute)).ConstructorArguments.First().Value;
				    var updater = typeof(NodesUpdater<>).MakeGenericType(updaterType).CreateInstance();
				    updaters.Add((IModelNodesGeneratorUpdater) updater);
				    var name =emitCached? nameof(NodesUpdater<ModelNodesGeneratorBase>.UpdateCached):nameof(NodesUpdater<ModelNodesGeneratorBase>.Update);
				    return ((IObservable<ModelNode>) updater.GetPropertyValue(name)).Cast<T>();
			    });

	    public static IObservable<T> WhenGeneratingModelNodes<T>(this ApplicationModulesManager manager,Expression<Func<IModelApplication,T>> selector=null,bool emitCached=false) where T : IEnumerable<IModelNode> 
		    => manager.WhenGeneratingModelNodes<T>(emitCached);

	    class NodesUpdater<T> : ModelNodesGeneratorUpdater<T> where T : ModelNodesGeneratorBase{
		    readonly Subject<ModelNode> _updateSubject = new();
		    readonly Subject<ModelNode> _updateCachedSubject = new();

		    public IObservable<ModelNode> Update => _updateSubject;
		    public IObservable<ModelNode> UpdateCached => _updateCachedSubject;

		    public override void UpdateNode(ModelNode node) => _updateSubject.OnNext(node);

		    public override void UpdateCachedNode(ModelNode node) => _updateCachedSubject.OnNext(node);
	    }

    }
}