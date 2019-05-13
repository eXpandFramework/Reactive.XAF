using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ViewExtensions{
        
        public static IObservable<T> WhenClosing<T>(this T view) where T : View{
            return Observable.Return(view).Closing();
        }

        public static IObservable<T> Closing<T>(this IObservable<T> source) where T:View{
            return source.Cast<View>().SelectMany(view => {
                return Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => view.Closing += handler,
                    handler => view.Closing -= handler)
                    .TakeUntil(view.WhenDisposingView().Select(unit => unit))
                    ;
            })
            .Select(pattern => (T)pattern.Sender);
        }
        
        public static IObservable<(T view, CancelEventArgs e)> WhenQueryCanClose<T>(this T view) where T : View{
            return Observable.Return(view).QueryCanClose();
        }

        public static IObservable<(T view, CancelEventArgs e)> QueryCanClose<T>(this IObservable<T> source) where T:View{
            return source.Cast<View>().SelectMany(view => {
                return Observable.FromEventPattern<EventHandler<CancelEventArgs>, CancelEventArgs>(
                    handler => view.QueryCanClose += handler,
                    handler => view.QueryCanClose -= handler)
                    .TakeUntil(view.WhenDisposingView().Select(unit => unit))
                    ;
            }).TransformPattern<CancelEventArgs,T>();
        }
        public static IObservable<(T view, CancelEventArgs e)> WhenQueryCanChangeCurrentObject<T>(this T view) where T : View{
            return Observable.Return(view).QueryCanClose();
        }

        public static IObservable<(T view, CancelEventArgs e)> QueryCanChangeCurrentObject<T>(this IObservable<T> source) where T:View{
            return source.Cast<View>().SelectMany(view => {
                return Observable.FromEventPattern<EventHandler<CancelEventArgs>, CancelEventArgs>(
                    handler => view.QueryCanChangeCurrentObject += handler,
                    handler => view.QueryCanChangeCurrentObject -= handler)
                    .TakeUntil(view.WhenDisposingView().Select(unit => unit))
                    ;
            }).TransformPattern<CancelEventArgs,T>();
        }

        public static IObservable<(T view, EventArgs e)> WhenControlsCreated<T>(this T view) where T : View{
            return Observable.Return(view).ControlsCreated();
        }

        public static IObservable<(T view, EventArgs e)> ControlsCreated<T>(this IObservable<T> source) where T:View{
            return source.Cast<View>().SelectMany(view => {
                return view.IsControlCreated
                    ? Observable.Return(new EventPattern<EventArgs>(view, EventArgs.Empty))
                    : Observable.FromEventPattern<EventHandler, EventArgs>(
                            handler => view.ControlsCreated += handler,
                            handler => view.ControlsCreated -= handler)
                        .TakeUntil(view.WhenDisposingView().Select(unit => unit));
            }).TransformPattern<EventArgs,T>();
        }

        public static IObservable<(T view, EventArgs e)> WhenSelectionChanged<T>(this T controller) where T : View{
            return Observable.Return(controller).SelectionChanged();
        }

        public static IObservable<(T view, EventArgs e)> SelectionChanged<T>(this IObservable<T> source) where T:View{
            return source
                .SelectMany(item => Observable.FromEventPattern<EventHandler, EventArgs>(
                        handler => item.SelectionChanged += handler,
                        handler => item.SelectionChanged -= handler)
                    .TakeUntil(WhenDisposingView(item))
                )
                .TransformPattern<EventArgs,T>();
        }

        public static IObservable<(T view, EventArgs e)> WhenCurrentObjectChanged<T>(this T controller) where T : View{
            return Observable.Return(controller).CurrentObjectChanged();
        }

        public static IObservable<(T view, EventArgs e)> CurrentObjectChanged<T>(this IObservable<T> source) where T:View{
            return source.SelectMany(item => {
                return Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => item.CurrentObjectChanged += handler,
                    handler => item.CurrentObjectChanged -= handler)
                    .TakeUntil(WhenDisposingView(item));
            }).TransformPattern<EventArgs,T>();
        }
        
        public static IObservable<Unit> WhenDisposingView<TView>(this TView view) where TView:View{
            return Observable.Return(view).Disposing();
        }

        public static IObservable<Unit> Disposing<TView>(this IObservable<TView> source) where TView:View{
            return source
                .SelectMany(item => Observable.StartAsync(async () => await Observable.FromEventPattern<CancelEventHandler, EventArgs>(handler => item.Disposing += handler,handler => item.Disposing -= handler)))
                .ToUnit();
        }

        public static IObservable<ListPropertyEditor> NestedListViews<TView>(this IObservable<TView> views, params Type[] objectTypes)
            where TView : DetailView{
            return views.ControlsCreated().SelectMany(_ => _.view.NestedListViews(objectTypes));
        }

        public static IObservable<ListPropertyEditor> NestedListViews<TView>(this TView view, params Type[] objectTypes ) where TView : DetailView{
            var listPropertyEditors = view.GetItems<ListPropertyEditor>().Where(editor =>editor.Frame?.View != null).ToObservable();
            var nestedEditors = listPropertyEditors.SelectMany(editor => {
                var detailView = ((ListView) editor.Frame.View).EditView;
                return detailView != null ? detailView.NestedListViews(objectTypes) : Observable.Never<ListPropertyEditor>();
            });
            return listPropertyEditors.Where(editor => objectTypes.Any(type => type.IsAssignableFrom(editor.Frame.View.ObjectTypeInfo.Type))).Merge(nestedEditors);
        }


        public static IObservable<TView> When<TView>(this IObservable<TView> source,Type objectType=null,ViewType viewType=ViewType.Any,Nesting nesting=Nesting.Any) where TView:View{
            objectType = objectType ?? typeof(object);
            return source.Where(view =>view.Fits(viewType,nesting,objectType));
        }

    }
}