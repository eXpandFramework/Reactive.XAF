using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.Utils.Extensions;
using DevExpress.XAF.Modules.Reactive.Extensions;

namespace DevExpress.XAF.Modules.Reactive.Services{
    public static class ViewExtensions{
        public static IObservable<(T view, EventArgs e)> WhenControlsCreated<T>(this T view) where T : View{
            return Observable.Return(view).ControlsCreated();
        }

        public static IObservable<(T view, EventArgs e)> ControlsCreated<T>(this IObservable<T> source) where T:View{
            return source.Cast<View>().SelectMany(item => {
                return Observable.FromEventPattern<EventHandler, EventArgs>(
                        handler => item.ControlsCreated += handler,
                        handler => item.ControlsCreated -= handler)
                    .TakeUntil(WhenDisposingView(item));
            }).TransformPattern<EventArgs,T>();
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
        
        public static IObservable<TView> WhenDisposingView<TView>(this TView view) where TView:View{
            return Disposing(Observable.Return(view));
        }

        public static IObservable<TView> Disposing<TView>(this IObservable<TView> source) where TView:View{
            return source.SelectMany(item => {
                return Observable.FromEventPattern<CancelEventHandler, EventArgs>(
                    handler => item.Disposing += handler,
                    handler => item.Disposing -= handler).Select(pattern => item);
            });
        }

        public static IObservable<ListPropertyEditor> NestedListViews<TView>(this IObservable<TView> views, params Type[] objectTypes)
            where TView : DetailView{
            return views.ControlsCreated().SelectMany(_ => _.view.NestedListViews(objectTypes));
        }

        public static IObservable<ListPropertyEditor> NestedListViews<TView>(this TView view, params Type[] objectTypes ) where TView : DetailView{
            var listPropertyEditors = view.GetItems<ListPropertyEditor>().Where(editor =>editor.Frame?.View != null).ToObservable();
            var nestedEditors = listPropertyEditors.SelectMany(editor => {
                var detailView = editor.Frame.View.CastTo<ListView>().EditView;
                return detailView != null ? detailView.CastTo<DetailView>().NestedListViews(objectTypes) : Observable.Never<ListPropertyEditor>();
            });
            return listPropertyEditors.Where(editor => objectTypes.Any(type => type.IsAssignableFrom(editor.Frame.View.ObjectTypeInfo.Type))).Merge(nestedEditors);
        }


        public static IObservable<TView> When<TView>(this IObservable<TView> source,Type objectType=null,ViewType viewType=ViewType.Any,Nesting nesting=Nesting.Any) where TView:View{
            objectType = objectType ?? typeof(object);
            return source.Where(view =>view.Fits(viewType,nesting,objectType));
        }

    }
}