using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
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
        public static IObservable<(NewObjectViewController sender, CollectTypesEventArgs e)> WhenCollectCreatableItemTypes(this NewObjectViewController controller) 
            => Observable.FromEventPattern<EventHandler<CollectTypesEventArgs>, CollectTypesEventArgs>(
                    h => controller.CollectCreatableItemTypes += h, h => controller.CollectCreatableItemTypes -= h, ImmediateScheduler.Instance)
                .TransformPattern<CollectTypesEventArgs, NewObjectViewController>();
        public static IObservable<ObjectCreatingEventArgs> WhenObjectCreating(this NewObjectViewController controller) 
            => Observable.FromEventPattern<EventHandler<ObjectCreatingEventArgs>, ObjectCreatingEventArgs>(
                    h => controller.ObjectCreating += h, h => controller.ObjectCreating -= h, ImmediateScheduler.Instance)
                .Select(pattern => pattern.EventArgs);
        
        public static IObservable<ObjectCreatedEventArgs> WhenObjectCreated(this NewObjectViewController controller) 
            => Observable.FromEventPattern<EventHandler<ObjectCreatedEventArgs>, ObjectCreatedEventArgs>(
                    h => controller.ObjectCreated += h, h => controller.ObjectCreated -= h, ImmediateScheduler.Instance)
                .Select(pattern => pattern.EventArgs);
        
        public static IObservable<(NewObjectViewController sender, CollectTypesEventArgs e)> WhenCollectDescendantTypes(this NewObjectViewController controller) 
            => Observable.FromEventPattern<EventHandler<CollectTypesEventArgs>, CollectTypesEventArgs>(
                    h => controller.CollectDescendantTypes += h, h => controller.CollectDescendantTypes -= h, ImmediateScheduler.Instance)
                .TransformPattern<CollectTypesEventArgs, NewObjectViewController>();

        public static IObservable<CollectTypesEventArgs> ReplaceTypes(this NewObjectViewController controller, params Type[] objectTypes)
            => controller.ReplaceTypes(null, objectTypes);
        
        public static IObservable<CollectTypesEventArgs>  ReplaceTypes(this NewObjectViewController controller,Func<ObjectCreatingEventArgs,IObservable<object>> modifyObject,params Type[] objectTypes)
            =>controller.WhenCollectDescendantTypes().Select(t => {
                    t.e.Types.Clear();
                    objectTypes.ForEach(type => t.e.Types.Add(type));
                    return t.e;
                })
                .Merge(controller.Defer(controller.UpdateNewObjectAction).IgnoreElements().To<CollectTypesEventArgs>())
                .Merge(controller.WhenObjectCreating().Where(e => objectTypes.Contains(e.ObjectType))
                    .SelectMany(e => {
                        e.ObjectSpace = e.ObjectType.ToTypeInfo().IsPersistent ? controller.View.ObjectSpace.CreateNestedObjectSpace()
                            : controller.Application.CreateObjectSpace(e.ObjectType);
                        e.ObjectSpace.To<CompositeObjectSpace>().PopulateAdditionalObjectSpaces(controller.Application);
                        e.NewObject = e.ObjectSpace.CreateObject(e.ObjectType);
                        return modifyObject?.Invoke(e)??Observable.Empty<object>();
                    })
                    .IgnoreElements().To<CollectTypesEventArgs>());
        
        public static IObservable<(NewObjectViewController sender, ProcessNewObjectEventArgs e)> WhenAddObjectToCollection(this NewObjectViewController controller) 
            => Observable.FromEventPattern<EventHandler<ProcessNewObjectEventArgs>, ProcessNewObjectEventArgs>(
                    h => controller.CustomAddObjectToCollection += h, h => controller.CustomAddObjectToCollection -= h, ImmediateScheduler.Instance)
                .TransformPattern<ProcessNewObjectEventArgs, NewObjectViewController>();

        public static IObservable<(NewObjectViewController sender, CollectTypesEventArgs e)> CollectCreatableItemTypes(this IObservable<NewObjectViewController> source)
            => source.SelectMany(controller => controller.WhenCollectCreatableItemTypes());
    }
}