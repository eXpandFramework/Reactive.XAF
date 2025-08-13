using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers{
    public static partial class ControllerExtensions{
        public static IObservable<Unit> UseObjectDefaultDetailView(this NewObjectViewController controller) 
            => controller.WhenObjectCreating().Do(e => e.Cancel = true)
                .ConcatToUnit(controller.NewObjectAction.WhenExecuted(e => {
                    var objectType = controller.View.ObjectTypeInfo.Type;
                    var objectSpace = controller.Application.CreateObjectSpace(objectType);
                    e.ShowViewParameters.CreatedView =
                        controller.Application.CreateDetailView(objectSpace, objectSpace.CreateObject(objectType));
                    return Unit.Default.Observe();
                }) );

        public static IObservable<Unit> ReplaceTypes<T>(this NewObjectViewController controller,Func<Frame,IObservable<object>> whenFrameResilient) 
            => controller.ReplaceTypes(e => controller.Application.WhenFrame(e.ObjectType).Take(1)
                .SelectManyItemResilient(whenFrameResilient),typeof(T)).ToUnit();
        
        public static IObservable<CollectTypesEventArgs> WhenCollectCreatableItemTypes(this NewObjectViewController controller) 
            => controller.ProcessEvent<CollectTypesEventArgs>(nameof(NewObjectViewController.CollectCreatableItemTypes));
        
        public static IObservable<ObjectCreatingEventArgs> WhenObjectCreating(this NewObjectViewController controller) 
            => controller.ProcessEvent<ObjectCreatingEventArgs>(nameof(NewObjectViewController.ObjectCreating));
        
        public static IObservable<ObjectCreatedEventArgs> WhenObjectCreated(this NewObjectViewController controller) 
            => controller.ProcessEvent<ObjectCreatedEventArgs>(nameof(NewObjectViewController.ObjectCreated));
        
        public static IObservable<CollectTypesEventArgs> WhenCollectDescendantTypes(this NewObjectViewController controller) 
            => controller.ProcessEvent<CollectTypesEventArgs>(nameof(NewObjectViewController.CollectDescendantTypes));

        public static IObservable<Unit> ReplaceNewObjectTypes<TParentObject>(this XafApplication application,Type listViewObjectType,params Type[] types)
            => application.WhenFrame(listViewObjectType,nesting:Nesting.Nested)
                .SelectUntilViewClosed(frame => frame.ParentObject() is TParentObject ? frame.ReplaceNewObjectTypes(types).ToUnit() : Observable.Empty<Unit>()).ToUnit();
        
        public static IObservable<CollectTypesEventArgs> ReplaceNewObjectTypes(this Frame frame, params Type[] objectTypes)
            => frame.GetController<NewObjectViewController>().ReplaceTypes(objectTypes);
        
        public static IObservable<CollectTypesEventArgs> ReplaceTypes(this NewObjectViewController controller, params Type[] objectTypes)
            => controller.ReplaceTypes(null, objectTypes);
        
        public static IObservable<CollectTypesEventArgs>  ReplaceTypes(this NewObjectViewController controller,Func<ObjectCreatingEventArgs,IObservable<object>> modifyObject,params Type[] objectTypes)
            =>controller.WhenCollectDescendantTypes().Select(e => {
                    e.Types.Except(e.Types.Where(objectTypes.Contains)).ToArray().Do(type => e.Types.Remove(type)).Enumerate();
                    objectTypes.Except(e.Types).Do(type => e.Types.Add(type)).Enumerate();
                    return e;
                })
                .Merge(controller.DeferItemResilient(() => controller.DeferAction(viewController => viewController.UpdateNewObjectAction()).IgnoreElements().To<CollectTypesEventArgs>()))
                .Merge(controller.WhenObjectCreating().Where(e => objectTypes.Contains(e.ObjectType))
                    .SelectManyItemResilient(e => {
                        if (e.NewObject != null) return modifyObject?.Invoke(e) ?? Observable.Empty<object>();
                        e.ObjectSpace = e.ObjectType.ToTypeInfo().IsPersistent ? controller.View.ObjectSpace.CreateNestedObjectSpace()
                            : controller.Application.CreateObjectSpace(e.ObjectType);
                        e.ObjectSpace.Cast<CompositeObjectSpace>().PopulateAdditionalObjectSpaces(controller.Application);
                        e.NewObject = e.ObjectSpace.CreateObject(e.ObjectType);
                        return modifyObject?.Invoke(e)??Observable.Empty<object>();
                    })
                    .IgnoreElements().To<CollectTypesEventArgs>())
                .PushStackFrame()
            ;
        
        public static IObservable<ProcessNewObjectEventArgs> WhenAddObjectToCollection(this NewObjectViewController controller) 
            => controller.ProcessEvent<ProcessNewObjectEventArgs>(nameof(NewObjectViewController.CustomAddObjectToCollection));

        public static IObservable<CollectTypesEventArgs> CollectCreatableItemTypes(this IObservable<NewObjectViewController> source)
            => source.SelectMany(controller => controller.WhenCollectCreatableItemTypes());
    }
}