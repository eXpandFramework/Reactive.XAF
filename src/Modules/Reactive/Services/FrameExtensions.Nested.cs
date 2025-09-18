using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtensions;

namespace Xpand.XAF.Modules.Reactive.Services;
    public partial class FrameExtensions {
        public static IObservable<TFrame> When<TFrame>(this IObservable<TFrame> source, TemplateContext templateContext) where TFrame : Frame 
            => source.Where(window => window.Context == templateContext);

        public static IObservable<Frame> When(this IObservable<Frame> source, Func<Frame,IEnumerable<IModelObjectView>> objectViewsSelector) 
            => source.Where(frame => objectViewsSelector(frame).Contains(frame.View.Model));
        
        public static IObservable<T> When<T>(this IObservable<T> source, Frame parentFrame, NestedFrame nestedFrame) 
            => source.Where(_ => nestedFrame?.View != null && parentFrame?.View != null);

        public static IObservable<(TFrame frame, ViewChangingEventArgs args)> WhenViewChanging<TFrame>(this TFrame source) where TFrame : Frame 
            => source.ProcessEvent<ViewChangingEventArgs>(nameof(Frame.ViewChanging)).InversePair(source);

        public static IObservable<(TFrame frame, ViewChangingEventArgs args)> ViewChanging<TFrame>(
            this IObservable<TFrame> source) where TFrame : Frame 
            => source.SelectMany(item => item.WhenViewChanging());
        
        public static IObservable<TFrame> ViewChanged<TFrame>(
            this IObservable<TFrame> source) where TFrame : Frame 
            => source.SelectMany(item => item.WhenViewChanged().Select(t => t.frame));

        public static IObservable<(TFrame frame, Frame source)> WhenViewChanged<TFrame>(this IObservable<TFrame> source) where TFrame : Frame
            => source.SelectMany(frame => frame.WhenViewChanged());
        
        public static IObservable<(TFrame frame, Frame source)> WhenViewChanged<TFrame>(this TFrame item) where TFrame : Frame 
            => item.ProcessEvent<ViewChangedEventArgs>(nameof(Frame.ViewChanged))
                .TakeUntil(item.WhenDisposedFrame()).Select(e => e.SourceFrame).InversePair(item);

        public static IObservable<TController> WhenController<TController>(this Frame frame) where TController : Controller
            => frame.Observe().Select(frame1 => frame1.GetController<TController>()).WhenNotDefault();
        
        public static IObservable<T> TemplateChanged<T>(this IObservable<T> source) where T : Frame 
            => source.SelectMany(item => item.Template != null ? item.Observe() : item.WhenTemplateChanged().Select(_ => item));

        public static IObservable<TFrame> WhenTemplateChanged<TFrame>(this TFrame item) where TFrame : Frame 
            => item.ProcessEvent(nameof(Frame.TemplateChanged))
                .TakeUntil(item.WhenDisposingFrame())
            ;

        public static IObservable<TFrame> WhenViewControllersActivated<TFrame>(this TFrame source) where TFrame : Frame
            => source.ProcessEvent(nameof(Frame.ViewControllersActivated)).To(source)
                .TakeUntil(source.WhenDisposedFrame());
        
        public static IObservable<TFrame> WhenTemplateViewChanged<TFrame>(this TFrame source) where TFrame : Frame 
            => source.ProcessEvent(nameof(Frame.TemplateViewChanged)).To(source)
                .TakeUntil(source.WhenDisposedFrame());

        public static IObservable<T> TemplateViewChanged<T>(this IObservable<T> source) where T : Frame 
            => source.SelectMany(item => item.WhenTemplateViewChanged().Select(_ => item));

        public static IObservable<Unit> WhenDisposingFrame<TFrame>(this TFrame source) where TFrame : Frame
            => source.ProcessEvent(nameof(Frame.Disposing)).TakeUntil(source.WhenDisposedFrame()).ToUnit();

        public static IObservable<Unit> WhenDisposedFrame<TFrame>(this TFrame source) where TFrame : Frame
            => source.ProcessEvent(nameof(Frame.Disposed)).ToUnit();

        public static IObservable<Unit> DisposingFrame<TFrame>(this IObservable<TFrame> source) where TFrame : Frame 
            => source.WhenNotDefault().SelectMany(item => item.WhenDisposingFrame()).ToUnit();

        public static IObservable<T> SelectUntilViewClosed<TFrame, T>(this IObservable<TFrame> source,
            Func<TFrame, IObservable<T>> selector) where TFrame : View
            => source.SelectMany(view => selector(view).TakeUntil(view.WhenClosed()));
        
        public static IObservable<T> SelectUntilViewClosed<T,TFrame>(this IObservable<TFrame> source, Func<TFrame, IObservable<T>> selector) where TFrame:Frame 
            => source.SelectMany(frame => selector(frame).TakeUntilViewClosed(frame));

        public static IObservable<TFrame> TakeUntilViewClosed<TFrame>(this IObservable<TFrame> source,Frame frame)  
            => source.TakeUntil(frame.WhenDisposedFrame());

        public static IObservable<NestedFrame> ToNestedFrame(this IObservable<ListPropertyEditor> source)
            => source.Select(editor => editor.Frame).Cast<NestedFrame>();

        public static IObservable<TFrame> WhenViewControllersActivated<TFrame>(this IObservable<TFrame> source) where TFrame : Frame
            => source.ConcatIgnored(frame => frame.WhenViewControllersActivated().Take(1));
        
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
        
        public static IObservable<SingleChoiceAction> ChangeViewVariant(this IObservable<Frame> source, string id) 
            => source
                .SelectMany(frame => frame.Actions("ChangeVariant").Cast<SingleChoiceAction>().ToNowObservable())
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
            => source.Where(frame => frame.When(nesting)).SelectMany(frame => frame.WhenFrame(viewType, objectType)) ;
        
        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, Func<Frame,Type> objectType = null,
            Func<Frame,ViewType> viewType = null, Nesting nesting = Nesting.Any) where T:Frame
            => source.WhenFrame(frame => frame.Observe().Cast<T>(),objectType,viewType,nesting) ;
        
        public static IObservable<T> WhenFrame<T>(this T frame, params string[] viewIds) where T : Frame 
            => frame.WhenViewChanged().To(frame).Where(_ => viewIds.Contains(frame.View.Id));

        private static IObservable<T> WhenFrame<T>(this T frame, ViewType viewType, Type type) where T : Frame
            => frame.WhenFrame(viewType,type, () => frame.Observe().Cast<T>());
        
        private static IObservable<TResult> WhenFrame<T,TResult>(this T frame,ViewType viewType, Type type,Func<IObservable<TResult>> resilientSelector) where T : Frame 
            => (frame.View != null ? frame.When(viewType) && frame.When(type) ? frame.Observe() : Observable.Empty<T>()
                : frame.WhenViewChanged().Where(t => t.frame.When(viewType) && t.frame.When(type)).To(frame))
                .SelectManyItemResilient(arg => resilientSelector()
                    .TakeUntil(arg.View.WhenClosed()),[frame.View?.Id,type,viewType]);

    }