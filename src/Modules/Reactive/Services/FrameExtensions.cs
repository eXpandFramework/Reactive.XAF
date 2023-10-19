using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class FrameExtensions{
        public static IObservable<TFrame> WhenModule<TFrame>(this IObservable<TFrame> source, Type moduleType) where TFrame : Frame 
            => source.Where(frame => frame.Application.Modules.FindModule(moduleType) != null);

        public static IObservable<TFrame> MergeViewCurrentObjectChanged<TFrame>(this IObservable<TFrame> source) where TFrame : Frame
            => source.SelectMany(frame => frame.View.WhenCurrentObjectChanged().WhenNotDefault(view => view.CurrentObject)
                .DistinctUntilChanged(view => view.ObjectSpace.GetKeyValue(view.CurrentObject))
                    .WhenNotDefault(view => view.CurrentObject).To(frame).WaitUntilInactive(3.Seconds()).ObserveOnContext());
        
        public static IObservable<TFrame> When<TFrame>(this IObservable<TFrame> source, TemplateContext templateContext) where TFrame : Frame 
            => source.Where(window => window.Context == templateContext);

        public static IObservable<Frame> When(this IObservable<Frame> source, Func<Frame,IEnumerable<IModelObjectView>> objectViewsSelector) 
            => source.Where(frame => objectViewsSelector(frame).Contains(frame.View.Model));
        
        public static IObservable<T> When<T>(this IObservable<T> source, Frame parentFrame, NestedFrame nestedFrame) 
            => source.Where(_ => nestedFrame?.View != null && parentFrame?.View != null);

        internal static IObservable<TFrame> WhenFits<TFrame>(this IObservable<TFrame> source, ActionBase action) where TFrame : Frame 
            => source.WhenFits(action.TargetViewType, action.TargetObjectType);

        internal static IObservable<TFrame> WhenFits<TFrame>(this IObservable<TFrame> source, ViewType viewType,
            Type objectType = null, Nesting nesting = Nesting.Any, bool? isPopupLookup = null) where TFrame : Frame 
            => source.SelectMany(frame => frame.View != null ? frame.Observe() : frame.WhenViewChanged().Select(_ => frame))
                .Where(frame => frame.View.Is(viewType, nesting, objectType))
                .Where(frame => {
                    if (!isPopupLookup.HasValue) return true;
                    var popupLookupTemplate = frame.Template is ILookupPopupFrameTemplate;
                    return isPopupLookup.Value ? popupLookupTemplate : !popupLookupTemplate;
                });

        public static IObservable<View> CreateNewObject(this Window window)
            => window.DashboardViewItems(ViewType.ListView).Select(item => item.Frame).ToNowObservable()
                .ToController<NewObjectViewController>().SelectMany(controller => controller.NewObjectAction.Trigger(window.Application
                    .RootView(controller.Frame.View.ObjectTypeInfo.Type, ViewType.DetailView)
                    .Select(detailView => detailView)));
        public static IObservable<TFrame> WhenViewRefreshExecuted<TFrame>(this TFrame source,
            Action<SimpleActionExecuteEventArgs> retriedExecution) where TFrame : Frame
            => source.GetController<RefreshController>().RefreshAction.WhenExecuted(retriedExecution).To(source);
        
        public static IObservable<(TFrame frame, ViewChangingEventArgs args)> WhenViewChanging<TFrame>(this TFrame source) where TFrame : Frame 
            => source.WhenEvent<ViewChangingEventArgs>(nameof(Frame.ViewChanging)).InversePair(source);

        public static IObservable<(TFrame frame, ViewChangingEventArgs args)> ViewChanging<TFrame>(
            this IObservable<TFrame> source) where TFrame : Frame 
            => source.SelectMany(item => item.WhenViewChanging());
        
        public static IObservable<TFrame> ViewChanged<TFrame>(
            this IObservable<TFrame> source) where TFrame : Frame 
            => source.SelectMany(item => item.WhenViewChanged().Select(t => t.frame));

        public static IObservable<(TFrame frame, Frame source)> WhenViewChanged<TFrame>(this IObservable<TFrame> source) where TFrame : Frame
            => source.SelectMany(frame => frame.WhenViewChanged());
        
        public static IObservable<(TFrame frame, Frame source)> WhenViewChanged<TFrame>(this TFrame item) where TFrame : Frame 
            => item.WhenEvent<ViewChangedEventArgs>(nameof(Frame.ViewChanged))
                .TakeUntil(item.WhenDisposedFrame()).Select(e => e.SourceFrame).InversePair(item);

        public static IObservable<T> TemplateChanged<T>(this IObservable<T> source) where T : Frame 
            => source.SelectMany(item => item.Template != null ? item.Observe() : item.WhenTemplateChanged().Select(_ => item));

        public static IObservable<TFrame> WhenTemplateChanged<TFrame>(this TFrame item) where TFrame : Frame 
            => item.WhenEvent(nameof(Frame.TemplateChanged)).Select(pattern => pattern).To(item)
                .TakeUntil(item.WhenDisposingFrame())
            ;

        public static IObservable<TFrame> WhenTemplateViewChanged<TFrame>(this TFrame source) where TFrame : Frame 
            => source.WhenEvent(nameof(Frame.TemplateViewChanged)).To(source)
                .TakeUntil(source.WhenDisposedFrame());

        public static IObservable<T> TemplateViewChanged<T>(this IObservable<T> source) where T : Frame 
            => source.SelectMany(item => item.WhenTemplateViewChanged().Select(_ => item));

        public static IObservable<TFrame> DisableSimultaneousModificationsException<TFrame>(this TFrame frame) where TFrame : Frame 
            => frame.Controllers.Cast<Controller>().Where(controller1 => controller1.Name=="DevExpress.ExpressApp.Win.SystemModule.LockController").Take(1).ToNowObservable()
                .SelectMany(controller1 => controller1.WhenEvent("CustomProcessSimultaneousModificationsException").TakeUntil(frame.WhenDisposedFrame())
                    .Do(args => args.EventArgs.Cast<HandledEventArgs>().Handled=true)).To(frame);

        public static IObservable<Unit> WhenDisposingFrame<TFrame>(this TFrame source) where TFrame : Frame
            => source.WhenEvent(nameof(Frame.Disposing)).TakeUntil(source.WhenDisposedFrame()).ToUnit();

        public static IObservable<Unit> WhenDisposedFrame<TFrame>(this TFrame source) where TFrame : Frame
            => source.WhenEvent(nameof(Frame.Disposed)).ToUnit();

        public static IObservable<Unit> DisposingFrame<TFrame>(this IObservable<TFrame> source) where TFrame : Frame 
            => source.WhenNotDefault().SelectMany(item => item.WhenDisposingFrame()).ToUnit();

        public static IObservable<T> SelectUntilViewClosed<TFrame, T>(this IObservable<TFrame> source,
            Func<TFrame, IObservable<T>> selector) where TFrame : View
            => source.SelectMany(view => selector(view).TakeUntil(view.WhenClosed()));
        
        public static IObservable<Window> CloseWindow<TFrame>(this IObservable<TFrame> source) where TFrame:Frame 
            => source.SelectMany(frame => frame.View.WhenActivated().To(frame).WaitUntilInactive(1.Seconds()).ObserveOnContext())
                .Cast<Window>().Do(frame => frame.Close());
        
        public static IObservable<T> SelectUntilViewClosed<T,TFrame>(this IObservable<TFrame> source, Func<TFrame, IObservable<T>> selector) where TFrame:Frame 
            => source.SelectMany(frame => selector(frame).TakeUntilViewClosed(frame));
        
        public static IObservable<T> SwitchUntilViewClosed<T,TFrame>(this IObservable<TFrame> source, Func<TFrame, IObservable<T>> selector) where TFrame:Frame 
            => source.Select(frame => selector(frame).TakeUntilViewClosed(frame)).Switch();
        
        public static IObservable<TFrame> TakeUntilViewClosed<TFrame>(this IObservable<TFrame> source,Frame frame)  
            => source.TakeUntil(frame.View.WhenClosing());
        
        public static IObservable<SimpleActionExecuteEventArgs> ShowInstanceDetailView(this IObservable<Frame> source,params  Type[] objectTypes) 
            => source.WhenFrame(objectTypes).WhenFrame(ViewType.ListView).ToController<ListViewProcessCurrentObjectController>().CustomProcessSelectedItem(true)
                .DoWhen(e => e.View().ObjectTypeInfo.Type.IsInstanceOfType(e.View().CurrentObject),
                    e => {
                        var currentObject = e.Action.View().CurrentObject;
                        var typeInfo = currentObject.GetType().ToTypeInfo();
                        var property = typeInfo.FindAttribute<ShowInstanceDetailViewAttribute>().Property;
                        if (property != null) {
                            currentObject = typeInfo.FindMember(property).GetValue(currentObject);
                        }
                        e.ShowViewParameters.CreatedView = e.Application().NewDetailView(space => space.GetObject(currentObject), currentObject.GetType().GetModelClass().DefaultDetailView);
                    });
        
        public static IObservable<ListPropertyEditor> NestedListViews(this Frame frame, params Type[] objectTypes ) 
            => frame.View.ToDetailView().NestedListViews(objectTypes);

        public static IEnumerable<Frame> WhenFrame<T>(this IEnumerable<T> source, Type objectType = null,
            ViewType viewType = ViewType.Any, Nesting nesting = Nesting.Any) where T:Frame
            => source.ToObservable(Transform.ImmediateScheduler).WhenFrame(objectType,viewType,nesting).ToEnumerable();
        
        public static IEnumerable<Frame> WhenFrame<T>(this IEnumerable<T> source, params string[] viewIds) where T:Frame 
            => source.ToObservable(Transform.ImmediateScheduler).Where(arg => viewIds.Contains(arg.View.Id)).ToEnumerable();

        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, params Type[] objectTypes) where T:Frame 
            => source.Where(frame => frame.When(objectTypes));
        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, params string[] viewIds) where T:Frame 
            => source.Where(frame => frame.When(viewIds));
        
        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, params Nesting[] nesting) where T:Frame 
            => source.Where(frame => frame.When(nesting));

        public static IObservable<View> ToView<T>(this IObservable<T> source) where T : Frame
            => source.Select(frame => frame.View);
        
        public static IObservable<Frame> OfView<TView>(this IObservable<Frame> source)
            => source.Where(item => item.View is TView);
        
        public static IObservable<SingleChoiceAction> ChangeViewVariant(this IObservable<Frame> source,string id) 
            => source.SelectMany(frame => frame.Actions("ChangeVariant").Cast<SingleChoiceAction>())
                .Do(action => action.DoExecute(action.Items.First(item => item.Id == id)))
                .Select(action => action);
        
        public static IObservable<DetailView> ToDetailView<T>(this IObservable<T> source) where T : Frame
            => source.Select(frame => frame.View.ToDetailView());
        
        public static IObservable<ListView> ToListView<T>(this IObservable<T> source) where T : Frame
            => source.Select(frame => frame.View.ToListView());
        
        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, params ViewType[] viewTypes) where T : Frame
            => source.Where(frame => frame.When(viewTypes));

        public static IObservable<View> WhenView<TFrame>(this IObservable<TFrame> source, Type objectType = null,
            ViewType viewType = ViewType.Any, Nesting nesting = Nesting.Any) where TFrame : Frame
            => source.WhenFrame(objectType, viewType, nesting).ToView();

        public static IObservable<View> WhenDetailView<TFrame>(this IObservable<TFrame> source, params Type[] objectTypes) where TFrame : Frame
            => source.WhenFrame(objectTypes).WhenFrame(ViewType.DetailView).ToView();
        
        public static IObservable<View> WhenDetailView<TFrame>(this IObservable<TFrame> source, Type objectType = null, Nesting nesting = Nesting.Any) where TFrame : Frame
            => source.WhenFrame(objectType,ViewType.DetailView, nesting).ToView();
        
        public static IObservable<View> WhenListView<TFrame>(this IObservable<TFrame> source, Type objectType = null, Nesting nesting = Nesting.Any) where TFrame : Frame
            => source.WhenFrame(objectType,ViewType.ListView, nesting).ToView();
        
        public static IObservable<T> WhenDetailView<T,TObject>(this IObservable<T> source, Func<TObject,bool> criteria) where T:Frame
            => source.WhenFrame(typeof(TObject),ViewType.DetailView).Where(frame => criteria(frame.View.CurrentObject.As<TObject>()));
        
        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, Type objectType = null,
            ViewType viewType = ViewType.Any, Nesting nesting = Nesting.Any) where T:Frame
            => source.Where(frame => frame.When(nesting))
                .SelectMany(frame => frame.WhenFrame(viewType, objectType));
        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, Func<Frame,Type> objectType = null,
            Func<Frame,ViewType> viewType = null, Nesting nesting = Nesting.Any) where T:Frame
            => source.Where(frame => frame.When(nesting))
                .SelectMany(frame => frame.WhenFrame(viewType?.Invoke(frame)??ViewType.Any, objectType?.Invoke(frame)));

        private static IObservable<T> WhenFrame<T>(this T frame,ViewType viewType, Type types) where T : Frame 
            => frame.View != null ? frame.When(viewType) && frame.When(types) ? frame.Observe() : Observable.Empty<T>()
                : frame.WhenViewChanged().Where(t => t.frame.When(viewType) && t.frame.When(types)).To(frame);
        
        public static IObservable<Frame> ListViewProcessSelectedItem(this IObservable<Frame> source,Action<SimpleActionExecuteEventArgs> executed=null) 
            => source.SelectMany(frame => frame.ListViewProcessSelectedItem(executed).Take(1));
        
        public static IObservable<Frame> ListViewProcessSelectedItem(this IObservable<Frame> source,string defaultFocusedItem) 
            => source.SelectMany(frame => frame.ListViewProcessSelectedItem(defaultFocusedItem).Take(1));

        public static IObservable<Frame> ListViewProcessSelectedItem(this Frame frame,string defaultFocusedItem) 
            => frame.ListViewProcessSelectedItem(e => e.ShowViewParameters.CreatedView.ToDetailView().SetDefaultFocusedItem(defaultFocusedItem));

        public static IObservable<Frame> ListViewProcessSelectedItem(this Frame frame,Action<SimpleActionExecuteEventArgs> executed) 
            => frame.ListViewProcessSelectedItem(() => frame.View.SelectedObjects.Cast<object>().First() ,executed);

        public static IObservable<Frame> ListViewProcessSelectedItem<T>(this Frame frame, Func<T> selectedObject,Action<SimpleActionExecuteEventArgs> executed=null){
            var action = frame.GetController<ListViewProcessCurrentObjectController>().ProcessCurrentObjectAction;
            var invoke = selectedObject.Invoke()??default(T);
            var afterNavigation = action.WhenExecuted().DoWhen(_ => executed != null, e => executed!(e))
                .SelectMany(e => frame.Application.WhenFrame(e.ShowViewParameters.CreatedView.ObjectTypeInfo.Type).Take(1));
            return action.Trigger(afterNavigation,invoke.YieldItem().Cast<object>().ToArray());
        }

    }
}