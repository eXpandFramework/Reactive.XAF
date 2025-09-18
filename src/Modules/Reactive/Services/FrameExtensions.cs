using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.ExpressionExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static partial class FrameExtensions{
        public static IObservable<TResult> WhenFrame<T,TResult>(this IObservable<T> source,Func<Frame,IObservable<TResult>> resilientSelector, Func<Frame,Type> objectType = null,
            Func<Frame,ViewType> viewType = null, Nesting nesting = Nesting.Any) where T:Frame
            => source.Where(frame => frame.When(nesting))
                .SelectMany(frame => frame.WhenFrame(viewType?.Invoke(frame)??ViewType.Any, objectType?.Invoke(frame),() => resilientSelector(frame)))
                .PushStackFrame();
        
        public static IObservable<TFrame> MergeCurrentObjectChanged<TFrame>(this IObservable<TFrame> source) where TFrame : Frame
            => source.SkipWhile(frame => frame.View==null).SelectMany(frame => frame.View.WhenCurrentObjectChanged().StartWith(frame.View).WhenNotDefault(view => view.CurrentObject)
                .DistinctUntilChanged(view => view.ObjectSpace.GetKeyValue(view.CurrentObject))
                    .WhenNotDefault(view => view.CurrentObject).To(frame)
                .WaitUntilInactive(2.Seconds()).ObserveOnContext().If(frame1 => !(frame1.IsDisposed() || frame1.View == null),frame1 => frame1.Observe()))
                .PushStackFrame();
        
        public static IObservable<TFrame> MergeObjectSpaceReloaded<TFrame>(this IObservable<TFrame> source) where TFrame : Frame
            => source.SkipWhile(frame => frame.View==null).SelectMany(frame => frame.View.ObjectSpace.WhenReloaded().To(frame).StartWith(frame)
                .WaitUntilInactive(2.Seconds()).ObserveOnContext().If(frame1 => !(frame1.IsDisposed() || frame1.View == null),frame1 => frame1.Observe()))
                .PushStackFrame();

        public static IObservable<TFrame> MergeObjectSpaceRefresh<TFrame>(this IObservable<TFrame> source) where TFrame : Frame
            => source.SkipWhile(frame => frame.View==null).SelectMany(frame => frame.View.ObjectSpace.WhenRefreshing().To(frame).StartWith(frame)
                .WaitUntilInactive(2.Seconds()).ObserveOnContext()
                .If(frame1 => !(frame1.IsDisposed() || frame1.View == null),frame1 => frame1.Observe()))
                .PushStackFrame();

        public static IObservable<Frame> MergeCurrentObjectModified<T>(this IObservable<Frame> source, params string[] properties)
            => source.SkipWhile(frame => frame.View == null).SelectMany(frame => frame.View.ObjectSpace
                .WhenModifiedObjects<T>(properties).To(frame)
                .WaitUntilInactive(2.Seconds()).ObserveOnContext()
                .If(frame1 => !(frame1.IsDisposed() || frame1.View == null),frame1 => frame1.Observe())
                .Finally(() => {})
                .StartWith(frame))
                .PushStackFrame();
        
        public static IObservable<Frame> MergeCurrentObjectModified<T>(this IObservable<Frame> source,params Expression<Func<T,object>>[] properties) 
            => source.MergeCurrentObjectModified<T>(properties.Select(expression => expression.MemberExpressionName()).ToArray())
                .PushStackFrame();
        
        internal static IObservable<TFrame> WhenFits<TFrame>(this IObservable<TFrame> source, ActionBase action) where TFrame : Frame 
            => source.WhenFits(action.TargetViewType, action.TargetObjectType)
                .PushStackFrame();

        internal static IObservable<TFrame> WhenFits<TFrame>(this IObservable<TFrame> source, ViewType viewType,
            Type objectType = null, Nesting nesting = Nesting.Any, bool? isPopupLookup = null) where TFrame : Frame 
            => source.SelectMany(frame => frame.View != null ? frame.Observe() : frame.WhenViewChanged().Select(_ => frame))
                .Where(frame => frame.View.Is(viewType, nesting, objectType))
                .Where(frame => {
                    if (!isPopupLookup.HasValue) return true;
                    var popupLookupTemplate = frame.Template is ILookupPopupFrameTemplate;
                    return isPopupLookup.Value ? popupLookupTemplate : !popupLookupTemplate;
                })
                .PushStackFrame();

        public static IObservable<View> CreateNewObject(this Window window)
            => window.DashboardViewItems(ViewType.ListView).Select(item => item.Frame).ToNowObservable()
                .ToController<NewObjectViewController>().SelectMany(controller => controller.NewObjectAction.Trigger(window.Application
                    .RootView(controller.Frame.View.ObjectTypeInfo.Type, ViewType.DetailView)
                    .Select(detailView => detailView)))
                .PushStackFrame();

        public static IObservable<TFrame> WhenViewRefreshExecuted<TFrame>(this TFrame source,
            Action<SimpleActionExecuteEventArgs> selector=null) where TFrame : Frame {
            var refreshAction = source.GetController<RefreshController>().RefreshAction;
            return refreshAction.WhenExecuted(args => {
                selector?.Invoke(args);
                return Observable.Empty<TFrame>();
            }).To(source)
            .PushStackFrame();
        }

        public static IObservable<TFrame> DisableSimultaneousModificationsException<TFrame>(this TFrame frame) where TFrame : Frame 
            => frame.Controllers.Cast<Controller>().Where(controller1 => controller1.Name=="DevExpress.ExpressApp.Win.SystemModule.LockController").Take(1).ToNowObservable()
                .SelectMany(controller1 => controller1.ProcessEvent<HandledEventArgs>("CustomProcessSimultaneousModificationsException").TakeUntil(frame.WhenDisposedFrame())
                    .Do(e => e.Handled=true)).To(frame)
                .PushStackFrame();

        public static IObservable<Window> CloseWindow<TFrame>(this IObservable<TFrame> source) where TFrame:Frame 
            => source.SelectMany(frame => frame.View.WhenViewActivated().To(frame).WaitUntilInactive(1.Seconds()).ObserveOnContext())
                .Cast<Window>().Do(frame => frame.Close())
                .PushStackFrame();
        
        public static IObservable<Frame> ListViewProcessSelectedItem(this IObservable<Frame> source,Action<SimpleActionExecuteEventArgs> executed=null) 
            => source.SelectMany(frame => frame.ListViewProcessSelectedItem(executed).Take(1))
                .PushStackFrame();
        
        public static IObservable<Frame> ListViewProcessSelectedItem(this IObservable<Frame> source,string defaultFocusedItem) 
            => source.SelectMany(frame => frame.ListViewProcessSelectedItem(defaultFocusedItem).Take(1))
                .PushStackFrame();

        public static IObservable<Frame> ListViewProcessSelectedItem(this Frame frame,string defaultFocusedItem) 
            => frame.ListViewProcessSelectedItem(e => e.ShowViewParameters.CreatedView.ToDetailView().SetDefaultFocusedItem(defaultFocusedItem))
                .PushStackFrame();

        public static IObservable<Frame> ListViewProcessSelectedItem(this Frame frame,Action<SimpleActionExecuteEventArgs> executed=null) 
            => frame.ListViewProcessSelectedItem(() => frame.View.SelectedObjects.Cast<object>().FirstOrDefault() ,executed)
                .PushStackFrame();
        
        public static IObservable<Frame> ListViewProcessSelectedItem<T>(this Frame frame, Func<T> selectedObject,Action<SimpleActionExecuteEventArgs> executed=null){
            var action = frame.GetController<ListViewProcessCurrentObjectController>().ProcessCurrentObjectAction;
            var invoke = selectedObject.Invoke()??default(T);
            var afterNavigation = action.WhenExecuted().DoWhen(_ => executed != null, e => executed!(e))
                .If(e => e.ShowViewParameters.CreatedView!=null,e => frame.Application.WhenFrame(e.ShowViewParameters.CreatedView.ObjectTypeInfo.Type).Take(1),e => e.Frame().Observe());
            return action.Trigger(afterNavigation,invoke.YieldItem().Cast<object>().ToArray())
                .PushStackFrame();
        }

        public static IObservable<Unit> SaveAndCloseObject(this Frame frame)
            => frame.SaveAndCloseAction().Trigger()
                .PushStackFrame();
        
        public static IObservable<Frame> NewObject(this Frame frame, Type objectType,bool saveAndClose=false,Func<Frame,IObservable<Unit>> detailview=null) 
            => frame.NewObjectAction().Trigger(frame.Application.WhenFrame(objectType,ViewType.DetailView).Take(1),() => frame.NewObjectAction().Items.First(item => (Type)item.Data==objectType))
                .ConcatIgnored(frame1 => detailview?.Invoke(frame1)?? Observable.Empty<Unit>())
                .If(_ => saveAndClose,frame1 => frame1.SaveAndCloseObject().To<Frame>().Concat(frame),frame1 => frame1.Observe())
                .PushStackFrame();
        
    }
}