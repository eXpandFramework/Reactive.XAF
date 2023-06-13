using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.TypesInfoExtensions;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers{
    public static partial class ControllerExtensions{
        public static IObservable<Unit> ReplaceTypes<T>(this NewObjectViewController controller,Func<Frame,IObservable<object>> whenFrame) 
            => controller.ReplaceTypes(e => controller.Application.WhenFrame(e.ObjectType).Take(1)
                .SelectMany(whenFrame),typeof(T)).ToUnit();
        
        public static IObservable<CollectTypesEventArgs> WhenCollectCreatableItemTypes(this NewObjectViewController controller) 
            => controller.WhenEvent<CollectTypesEventArgs>(nameof(NewObjectViewController.CollectCreatableItemTypes));
        
        public static IObservable<ObjectCreatingEventArgs> WhenObjectCreating(this NewObjectViewController controller) 
            => controller.WhenEvent<ObjectCreatingEventArgs>(nameof(NewObjectViewController.ObjectCreating));
        
        public static IObservable<ObjectCreatedEventArgs> WhenObjectCreated(this NewObjectViewController controller) 
            => controller.WhenEvent<ObjectCreatedEventArgs>(nameof(NewObjectViewController.ObjectCreated));
        
        public static IObservable<CollectTypesEventArgs> WhenCollectDescendantTypes(this NewObjectViewController controller) 
            => controller.WhenEvent<CollectTypesEventArgs>(nameof(NewObjectViewController.CollectDescendantTypes));

        public static IObservable<CollectTypesEventArgs> ReplaceTypes(this NewObjectViewController controller, params Type[] objectTypes)
            => controller.ReplaceTypes(null, objectTypes);
        
        public static IObservable<CollectTypesEventArgs>  ReplaceTypes(this NewObjectViewController controller,Func<ObjectCreatingEventArgs,IObservable<object>> modifyObject,params Type[] objectTypes)
            =>controller.WhenCollectDescendantTypes().Select(e => {
                    e.Types.Clear();
                    objectTypes.ForEach(type => e.Types.Add(type));
                    return e;
                })
                .Merge(controller.DeferAction(viewController => viewController.UpdateNewObjectAction()).IgnoreElements().To<CollectTypesEventArgs>())
                .Merge(controller.WhenObjectCreating().Where(e => objectTypes.Contains(e.ObjectType))
                    .SelectMany(e => {
                        e.ObjectSpace = e.ObjectType.ToTypeInfo().IsPersistent ? controller.View.ObjectSpace.CreateNestedObjectSpace()
                            : controller.Application.CreateObjectSpace(e.ObjectType);
                        e.ObjectSpace.Cast<CompositeObjectSpace>().PopulateAdditionalObjectSpaces(controller.Application);
                        e.NewObject = e.ObjectSpace.CreateObject(e.ObjectType);
                        return modifyObject?.Invoke(e)??Observable.Empty<object>();
                    })
                    .IgnoreElements().To<CollectTypesEventArgs>());
        
        public static IObservable<ProcessNewObjectEventArgs> WhenAddObjectToCollection(this NewObjectViewController controller) 
            => controller.WhenEvent<ProcessNewObjectEventArgs>(nameof(NewObjectViewController.CustomAddObjectToCollection));

        public static IObservable<CollectTypesEventArgs> CollectCreatableItemTypes(this IObservable<NewObjectViewController> source)
            => source.SelectMany(controller => controller.WhenCollectCreatableItemTypes());
    }
}