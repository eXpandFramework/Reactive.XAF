using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ViewExtensions{
        public static DetailView AsDetailView(this View view) => view as DetailView;
        public static ListView AsListView(this View view) => view as ListView;
        public static ObjectView AsObjectView(this View view) => view as ObjectView;
        public static DashboardView AsDashboardView(this View view) => view as DashboardView;

        public static IObservable<T> WhenClosing<T>(this T view) where T : View => Observable.Return(view).WhenNotDefault().Closing();

        public static IObservable<T> Closing<T>(this IObservable<T> source) where T:View =>
            source.Cast<View>().SelectMany(view => Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => view.Closing += handler,
                    handler => view.Closing -= handler,ImmediateScheduler.Instance))
                .Select(pattern => (T)pattern.Sender);
        public static IObservable<T> WhenActivated<T>(this T view) where T : View => Observable.Return(view).Activated();

        public static IObservable<T> Activated<T>(this IObservable<T> source) where T:View =>
            source.Cast<View>().SelectMany(view => Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => view.Activated += handler,
                    handler => view.Activated -= handler,ImmediateScheduler.Instance))
                .Select(pattern => (T)pattern.Sender);

        public static IObservable<T> WhenClosed<T>(this T view) where T : View => view == null ? Observable.Empty<T>() : Observable.Return(view).Closed();

        public static IObservable<T> Closed<T>(this IObservable<T> source) where T:View =>
            source.SelectMany(view => Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => view.Closed += handler,
                    handler => view.Closed -= handler,ImmediateScheduler.Instance))
                .Select(pattern => (T)pattern.Sender);

        public static IObservable<(T view, CancelEventArgs e)> WhenQueryCanClose<T>(this T view) where T : View => Observable.Return(view).QueryCanClose();

        public static IObservable<(T view, CancelEventArgs e)> QueryCanClose<T>(this IObservable<T> source) where T:View =>
            source.Cast<View>().SelectMany(view => Observable.FromEventPattern<EventHandler<CancelEventArgs>, CancelEventArgs>(
                handler => view.QueryCanClose += handler,
                handler => view.QueryCanClose -= handler,ImmediateScheduler.Instance)).TransformPattern<CancelEventArgs,T>();
        public static IObservable<(T view, CancelEventArgs e)> WhenQueryCanChangeCurrentObject<T>(this T view) where T : View => Observable.Return(view).QueryCanClose();

        public static IObservable<(T view, CancelEventArgs e)> QueryCanChangeCurrentObject<T>(this IObservable<T> source) where T:View =>
            source.Cast<View>().SelectMany(view => Observable.FromEventPattern<EventHandler<CancelEventArgs>, CancelEventArgs>(
                handler => view.QueryCanChangeCurrentObject += handler,
                handler => view.QueryCanChangeCurrentObject -= handler,ImmediateScheduler.Instance)).TransformPattern<CancelEventArgs,T>();


        public static IObservable<T> WhenControlsCreated<T>(this T view) where T : View =>
            Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => view.ControlsCreated += handler, handler => view.ControlsCreated -= handler,ImmediateScheduler.Instance)
                .Select(pattern => pattern.Sender).Cast<T>();

        public static IObservable<T> ControlsCreated<T>(this IObservable<T> source) where T:View =>
            source.SelectMany(view => Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => view.ControlsCreated += handler, handler => view.ControlsCreated -= handler, ImmediateScheduler.Instance)
                .Select(pattern => pattern.Sender).Cast<T>());

        public static IObservable<T> WhenSelectionChanged<T>(this T view) where T : View => view.ReturnObservable().SelectionChanged();

        public static IObservable<T> SelectionChanged<T>(this IObservable<T> source) where T:View =>
            source.SelectMany(item => Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => item.SelectionChanged += handler,
                    handler => item.SelectionChanged -= handler,ImmediateScheduler.Instance))
                .Select(pattern => pattern.Sender).Cast<T>();

        public static IObservable<T> WhenCurrentObjectChanged<T>(this T controller) where T : View => Observable.Return(controller).CurrentObjectChanged();

        public static IObservable<T> CurrentObjectChanged<T>(this IObservable<T> source) where T:View =>
            source.SelectMany(item => Observable.FromEventPattern<EventHandler, EventArgs>(
                handler => item.CurrentObjectChanged += handler,
                handler => item.CurrentObjectChanged -= handler,ImmediateScheduler.Instance)).TransformPattern<T>();

        public static IObservable<Unit> WhenDisposingView<TView>(this TView view) where TView:View => Observable.Return(view).Disposing();

        public static IObservable<Unit> Disposing<TView>(this IObservable<TView> source) where TView:View =>
            source.SelectMany(item => Observable.FromEventPattern<CancelEventHandler, EventArgs>(handler => item.Disposing += handler,
                        handler => item.Disposing -= handler, ImmediateScheduler.Instance)).ToUnit();

        public static IObservable<ListPropertyEditor> NestedListViews<TView>(this IObservable<TView> views, params Type[] objectTypes) where TView : DetailView => views.ControlsCreated().SelectMany(detailView => detailView.NestedListViews(objectTypes));

        public static IObservable<ListPropertyEditor> NestedListViews<TView>(this TView view, params Type[] objectTypes ) where TView : DetailView{
            var listPropertyEditors = view.GetItems<ListPropertyEditor>().Where(editor =>editor.Frame?.View != null).ToObservable();
            var nestedEditors = listPropertyEditors.SelectMany(editor => {
                var detailView = ((ListView) editor.Frame.View).EditView;
                return detailView != null ? detailView.NestedListViews(objectTypes) : Observable.Never<ListPropertyEditor>();
            });
            return listPropertyEditors.Where(editor => objectTypes.Any(type => type.IsAssignableFrom(editor.Frame.View.ObjectTypeInfo.Type))).Merge(nestedEditors);
        }


        public static IObservable<TView> When<TView>(this IObservable<TView> source,Type objectType=null,ViewType viewType=ViewType.Any,Nesting nesting=Nesting.Any) where TView:View{
            objectType ??= typeof(object);
            return source.Where(view =>view.Fits(viewType,nesting,objectType));
        }

    }
}