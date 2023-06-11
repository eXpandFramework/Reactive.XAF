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
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class FrameExtensions{
        public static IObservable<TFrame> WhenModule<TFrame>(
            this IObservable<TFrame> source, Type moduleType) where TFrame : Frame 
            => source.Where(_ => _.Application.Modules.FindModule(moduleType) != null);

        public static IObservable<TFrame> MergeViewCurrentObjectChanged<TFrame>(this IObservable<TFrame> source) where TFrame : Frame
            => source.SelectMany(frame => frame.View.WhenCurrentObjectChanged().DistinctUntilChanged(view => view.ObjectSpace.GetKeyValue(view.CurrentObject))
                    .WhenNotDefault(view => view.CurrentObject).To(frame).WaitUntilInactive(3.Seconds()).ObserveOnContext())
                ;
        
        public static IObservable<TFrame> When<TFrame>(this IObservable<TFrame> source, TemplateContext templateContext)
            where TFrame : Frame 
            => source.Where(window => window.Context == templateContext);

        public static IObservable<Frame> When(this IObservable<Frame> source, Func<Frame,IEnumerable<IModelObjectView>> objectViewsSelector) 
            => source.Where(frame => objectViewsSelector(frame).Contains(frame.View.Model));
        
        public static IObservable<T> When<T>(this IObservable<T> source, Frame parentFrame, NestedFrame nestedFrame) 
            => source.Where(_ => nestedFrame?.View != null && parentFrame?.View != null);

        internal static IObservable<TFrame> WhenFits<TFrame>(this IObservable<TFrame> source, ActionBase action)
            where TFrame : Frame 
            => source.WhenFits(action.TargetViewType, action.TargetObjectType);

        internal static IObservable<TFrame> WhenFits<TFrame>(this IObservable<TFrame> source, ViewType viewType,
            Type objectType = null, Nesting nesting = Nesting.Any, bool? isPopupLookup = null) where TFrame : Frame 
            => source.SelectMany(frame => frame.View != null ? frame.Observe() : frame.WhenViewChanged().Select(_ => frame))
                .Where(frame => frame.View.Is(viewType, nesting, objectType))
                .Where(_ => {
                    if (isPopupLookup.HasValue){
                        var popupLookupTemplate = _.Template is ILookupPopupFrameTemplate;
                        return isPopupLookup.Value ? popupLookupTemplate : !popupLookupTemplate;
                    }

                    return true;
                });

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
                .TakeUntil(item.WhenDisposingFrame()).Select(e => e.SourceFrame).InversePair(item);

        public static IObservable<T> TemplateChanged<T>(this IObservable<T> source) where T : Frame 
            => source.SelectMany(item => item.Template != null
                    ? item.Observe() : item.WhenEvent(nameof(Frame.TemplateChanged))
                        .TakeUntil(item.WhenDisposingFrame()).Select(_ => item));

        public static IObservable<TFrame> WhenTemplateChanged<TFrame>(this TFrame source) where TFrame : Frame 
            => source.Observe().TemplateChanged();

        public static IObservable<TFrame> WhenTemplateViewChanged<TFrame>(this TFrame source) where TFrame : Frame 
            => source.WhenEvent(nameof(Frame.TemplateViewChanged)).To(source)
                .TakeUntil(source.WhenDisposedFrame());

        public static IObservable<T> TemplateViewChanged<T>(this IObservable<T> source) where T : Frame 
            => source.SelectMany(item => item.WhenTemplateViewChanged().Select(_ => item));

        public static IObservable<TFrame> DisableSimultaneousModificationsException<TFrame>(this TFrame frame) where TFrame : Frame 
            => frame.Controllers.Cast<Controller>().Where(controller1 => controller1.Name=="DevExpress.ExpressApp.Win.SystemModule.LockController").Take(1).ToNowObservable()
                .SelectMany(controller1 => controller1.WhenEvent<HandledEventArgs>("CustomProcessSimultaneousModificationsException").Do(args => args.Handled=true))
                .To(frame);

        public static IObservable<Unit> WhenDisposingFrame<TFrame>(this TFrame source) where TFrame : Frame 
            => source.WhenEvent(nameof(Frame.Disposing)).ToUnit();
        
        public static IObservable<Unit> WhenDisposedFrame<TFrame>(this TFrame source) where TFrame : Frame 
            => source.WhenEvent(nameof(Frame.Disposed)).ToUnit();

        public static IObservable<Unit> DisposingFrame<TFrame>(this IObservable<TFrame> source) where TFrame : Frame 
            => source.WhenNotDefault().SelectMany(item => item.WhenDisposingFrame()).ToUnit();
        
        

        public static IObservable<T> SelectUntilViewClosed<TFrame, T>(this IObservable<TFrame> source,
            Func<TFrame, IObservable<T>> selector) where TFrame : View
            => source.SelectMany(view => selector(view).TakeUntil(view.WhenClosed()));
        
        public static IObservable<T> SelectUntilViewClosed<T,TFrame>(this IObservable<TFrame> source, Func<TFrame, IObservable<T>> selector) where TFrame:Frame 
            => source.SelectMany(frame => selector(frame).TakeUntilViewClosed(frame));
        
        public static IObservable<T> SwitchUntilViewClosed<T,TFrame>(this IObservable<TFrame> source, Func<TFrame, IObservable<T>> selector) where TFrame:Frame 
            => source.Select(frame => selector(frame).TakeUntilViewClosed(frame)).Switch();
        
        public static IObservable<TFrame> TakeUntilViewClosed<TFrame>(this IObservable<TFrame> source,Frame frame)  
            => source.TakeUntil(frame.View.WhenClosing());
        
        public static IObservable<SimpleActionExecuteEventArgs> ShowInstanceDetailView(this IObservable<Frame> source,params  Type[] objectTypes) 
            => source.WhenFrame(objectTypes).WhenFrame(ViewType.ListView).ToController<ListViewProcessCurrentObjectController>().CustomProcessSelectedItem(true)
                .DoWhen(e => e.View().ObjectTypeInfo.Type.IsInstanceOfType(e.View().CurrentObject),
                    e => e.ShowViewParameters.CreatedView = e.Application().NewDetailView(space => space.GetObject(e.Action.View().CurrentObject),
                        e.View().CurrentObject.GetType().GetModelClass().DefaultDetailView));
        
        public static IObservable<ListPropertyEditor> NestedListViews(this Frame frame, params Type[] objectTypes ) 
            => frame.View.ToDetailView().NestedListViews(objectTypes);

        public static IObservable<Frame> ListViewProcessSelectedItem(this IObservable<Frame> source) 
            => source.SelectMany(frame => frame.ListViewProcessSelectedItem().FirstAsync());

        public static IObservable<Frame> ListViewProcessSelectedItem(this Frame frame) {
            var action = frame.GetController<ListViewProcessCurrentObjectController>().ProcessCurrentObjectAction;
            var afterNavigation = action.WhenExecuted().SelectMany(e =>
                frame.Application.WhenFrame(e.ShowViewParameters.CreatedView.ObjectTypeInfo.Type).Take(1));
            return action.Trigger(afterNavigation);
        }
    }
}