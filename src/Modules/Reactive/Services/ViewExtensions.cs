using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.ViewExtensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ViewExtensions{
        public static IObservable<CustomizeShowViewParametersEventArgs> WhenNestedListViewProcessCustomizeShowViewParameters(
            this DetailView detailView, params Type[] objectTypes) 
            => detailView.FrameContainers(objectTypes).ToNowObservable()
                .SelectMany(frame => frame.GetController<ListViewProcessCurrentObjectController>()
                    .WhenEvent(nameof(ListViewProcessCurrentObjectController.CustomizeShowViewParameters))
                    .Select(pattern => pattern.EventArgs).Cast<CustomizeShowViewParametersEventArgs>());

        public static IObservable<T> WhenClosing<T>(this T view) where T : View 
            => view.ReturnObservable().WhenNotDefault().Closing();

        public static IObservable<T> Closing<T>(this IObservable<T> source) where T:View 
            => source.Cast<View>().SelectMany(view => Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => view.Closing += handler,
                    handler => view.Closing -= handler,ImmediateScheduler.Instance))
                .Select(pattern => (T)pattern.Sender);

        public static IObservable<T> WhenActivated<T>(this T view) where T : View 
            => Observable.FromEventPattern<EventHandler, EventArgs>(
                handler => view.Activated += handler,
                handler => view.Activated -= handler,ImmediateScheduler.Instance)
                .Select(pattern => (T)pattern.Sender);

        public static IObservable<T> Activated<T>(this IObservable<T> source) where T:View 
            => source.SelectMany(view => view.WhenActivated());
        
        public static IObservable<T> WhenModelChanged<T>(this T view) where T : View 
            => view.ReturnObservable().ModelChanged();

        public static IObservable<T> ModelChanged<T>(this IObservable<T> source) where T:View 
            => source.Cast<View>().SelectMany(view => Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => view.ModelChanged += handler,
                    handler => view.ModelChanged -= handler,ImmediateScheduler.Instance))
                .Select(pattern => (T)pattern.Sender);

        public static IObservable<T> WhenClosed<T>(this T view) where T : View 
            => view == null ? Observable.Empty<T>() : view.ReturnObservable().Closed();

        public static IObservable<T> Closed<T>(this IObservable<T> source) where T:View 
            => source.SelectMany(view => Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => view.Closed += handler,
                    handler => view.Closed -= handler,ImmediateScheduler.Instance))
                .Select(pattern => (T)pattern.Sender);

        public static IObservable<(T view, CancelEventArgs e)> WhenQueryCanClose<T>(this T view) where T : View 
            => view.ReturnObservable().QueryCanClose();

        public static IObservable<(T view, CancelEventArgs e)> QueryCanClose<T>(this IObservable<T> source) where T:View 
            => source.Cast<View>().SelectMany(view => Observable.FromEventPattern<EventHandler<CancelEventArgs>, CancelEventArgs>(
                handler => view.QueryCanClose += handler,
                handler => view.QueryCanClose -= handler,ImmediateScheduler.Instance)).TransformPattern<CancelEventArgs,T>();

        public static IObservable<(T view, CancelEventArgs e)> WhenQueryCanChangeCurrentObject<T>(this T view) where T : View 
            => view.ReturnObservable().QueryCanClose();

        public static IObservable<(T view, CancelEventArgs e)> QueryCanChangeCurrentObject<T>(this IObservable<T> source) where T:View 
            => source.Cast<View>().SelectMany(view => Observable.FromEventPattern<EventHandler<CancelEventArgs>, CancelEventArgs>(
                handler => view.QueryCanChangeCurrentObject += handler,
                handler => view.QueryCanChangeCurrentObject -= handler,ImmediateScheduler.Instance)).TransformPattern<CancelEventArgs,T>();

        public static IObservable<T> WhenControlsCreated<T>(this T view) where T : View 
            => Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => view.ControlsCreated += handler, handler => view.ControlsCreated -= handler,ImmediateScheduler.Instance)
                .Select(pattern => pattern.Sender)
                .Cast<T>();

        public static IObservable<T> WhenControlsCreated<T>(this IObservable<T> source) where T:View 
            => source.SelectMany(view => Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => view.ControlsCreated += handler, handler => view.ControlsCreated -= handler, ImmediateScheduler.Instance)
                .Select(pattern => pattern.Sender).Cast<T>());

        public static IObservable<T> WhenSelectionChanged<T>(this T view,int waitUntilInactiveSeconds=0) where T : View 
            => view.ReturnObservable().WhenNotDefault().SelectionChanged(waitUntilInactiveSeconds);

        public static IObservable<T> SelectionChanged<T>(this IObservable<T> source,int waitUntilInactiveSeconds=0) where T:View 
            => source.SelectMany(item => Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => item.SelectionChanged += handler,
                    handler => item.SelectionChanged -= handler,ImmediateScheduler.Instance))
                .Select(pattern => pattern.Sender).Cast<T>()
                .Publish(changed => waitUntilInactiveSeconds > 0 ? changed.ObserveOnContext() : changed)
                
        ;

        public static IObservable<T> WhenCurrentObjectChanged<T>(this T controller) where T : View 
            => controller.ReturnObservable().WhenNotDefault().CurrentObjectChanged();

        public static IObservable<T> CurrentObjectChanged<T>(this IObservable<T> source) where T:View 
            => source.SelectMany(item => Observable.FromEventPattern<EventHandler, EventArgs>(
                handler => item.CurrentObjectChanged += handler,
                handler => item.CurrentObjectChanged -= handler,ImmediateScheduler.Instance)).TransformPattern<T>();

        public static IObservable<Unit> WhenDisposingView<TView>(this TView view) where TView:View 
            => view.ReturnObservable().Disposing();

        public static IObservable<Unit> Disposing<TView>(this IObservable<TView> source) where TView:View 
            => source.SelectMany(item => Observable.FromEventPattern<CancelEventHandler, EventArgs>(handler => item.Disposing += handler,
                        handler => item.Disposing -= handler, ImmediateScheduler.Instance)).ToUnit();

        public static IObservable<ListPropertyEditor> NestedListViews<TView>(this IObservable<TView> views, params Type[] objectTypes) where TView : DetailView 
            => views.WhenControlsCreated().SelectMany(detailView => detailView.NestedListViews(objectTypes));

        public static IObservable<ListPropertyEditor> NestedListViews<TView>(this TView view, params Type[] objectTypes ) where TView : DetailView 
            => view.NestedViewItems<TView,ListPropertyEditor>(objectTypes);
        
        public static IObservable<TViewItem> NestedViewItems<TView,TViewItem>(this TView view, params Type[] objectTypes ) where TView : DetailView where TViewItem:ViewItem,IFrameContainer 
            => view.NestedFrameContainers(objectTypes).OfType<TViewItem>();
        
        public static IObservable<IFrameContainer> NestedFrameContainers<TView>(this TView view, params Type[] objectTypes ) where TView : DetailView  
            => view.GetItems<IFrameContainer>().Where(editor =>editor.Frame?.View == null).ToNowObservable()
                .SelectMany(frameContainer =>frameContainer.To<ViewItem>().Control==null? frameContainer.To<ViewItem>().WhenControlCreated().To(frameContainer):frameContainer.ReturnObservable())
                .NestedFrameContainers(view, objectTypes);
        public static IObservable<DashboardViewItem> NestedDashboards<TView>(this TView view, params Type[] objectTypes ) where TView : DetailView 
            => view.NestedViewItems<TView,DashboardViewItem>( objectTypes);

        private static IObservable<TFrameContainer> NestedFrameContainers<TView,TFrameContainer>(this IObservable<TFrameContainer> lazyListPropertyEditors, TView view, Type[] objectTypes) where TView : DetailView where TFrameContainer:IFrameContainer{
            var listFrameContainers = view.GetItems<ViewItem>().OfType<TFrameContainer>().Where(editor => editor.Frame?.View != null)
                .ToNowObservable().Merge(lazyListPropertyEditors);

            var nestedEditors = listFrameContainers.WhenNotDefault(container => container.Frame).SelectMany(frameContainer => {
                var detailView = ((ListView)frameContainer.Frame.View).EditView;
                return detailView != null ? detailView.NestedFrameContainers(objectTypes).OfType<TFrameContainer>() : Observable.Never<TFrameContainer>();
            });
            return listFrameContainers.WhenNotDefault(container => container.Frame)
                .Where(frameContainer => objectTypes.Any(type => type.IsAssignableFrom(frameContainer.Frame.View.ObjectTypeInfo.Type)))
                .Merge(nestedEditors);
        }

        public static IObservable<TView> When<TView>(this IObservable<TView> source,Type objectType=null,ViewType viewType=ViewType.Any,Nesting nesting=Nesting.Any) where TView:View{
            objectType ??= typeof(object);
            return source.Where(view =>view.Is(viewType,nesting,objectType));
        }
        

    }
}