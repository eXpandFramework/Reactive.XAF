using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class DetailViewExtensions{
        public static IObservable<(DetailView detailView, CancelEventArgs e)> WhenViewEditModeChanging(this DetailView detailView){
            return Observable.FromEventPattern<EventHandler<CancelEventArgs>, CancelEventArgs>(
                    h => detailView.ViewEditModeChanging += h, h => detailView.ViewEditModeChanging -= h,ImmediateScheduler.Instance)
                .TransformPattern<CancelEventArgs, DetailView>();
        }

        public static IObservable<(DetailView detailView, CancelEventArgs e)> ViewEditModeChanging<T>(this IObservable<T> source) where T : DetailView{
            return source.SelectMany(_ => _.WhenViewEditModeChanging());
        }
        
        public static TEditor GetPropertyEditor<TEditor, TObject>(this DetailView detailView, Expression<Func<TObject, object>> memberName) where TEditor : class{
            return detailView.GetPropertyEditor(((MemberExpression) memberName.Body).Member.Name) as TEditor;
        }
        
        public static PropertyEditor GetPropertyEditor(this DetailView detailView, string memberName){
            return detailView.GetItems<PropertyEditor>().First(editor => editor.MemberInfo.Name ==memberName);
        }
        
        public static ListPropertyEditor GetListPropertyEditor< TObject>(this DetailView detailView, Expression<Func<TObject, object>> memberName){
            return detailView.GetPropertyEditor<ListPropertyEditor,TObject>(memberName);
        }

        public static IObservable<(DetailView detailView, NestedFrame nestedFrame)>
            WhenChildren<TParentObject>(this IObservable<DetailView> source,
                params Type[] nestedObjectTypes){
            return source
                .Where(view => typeof(TParentObject).IsAssignableFrom(view.ObjectTypeInfo.Type))
                .SelectMany(detailView => {
                    var viewItems = detailView.GetItems<ViewItem>().OfType<IFrameContainer>().Cast<ViewItem>()
                        .ToObservable();
                    if (detailView.IsRoot)
                        return viewItems
                            .ControlCreated()
                            .ToNestedFrames(nestedObjectTypes)
                            .Select(_ => (detailView, _.nestedFrame));
                    return viewItems
                        .ToNestedFrames(nestedObjectTypes)
                        .Select(_ => (detailView, _.nestedFrame));
                });
        }

        public static IObservable<(DetailView detailView, NestedFrame nestedFrame)> WhenChildrenCurrentObjectChanged<TParentObject>(this IObservable<DetailView> source,params Type[] nestedObjectTypes){
            return source
                .Where(view => typeof(TParentObject).IsAssignableFrom(view.ObjectTypeInfo.Type))
                .SelectMany(detailView => {
                    var viewItems = detailView.GetItems<ViewItem>().OfType<IFrameContainer>().Cast<ViewItem>().ToObservable();
                    if (detailView.IsRoot)
                        return viewItems
                            .ControlCreated()
                            .ToNestedFrames(nestedObjectTypes)
                            .Select(_ => (detailView,_.nestedFrame));
//                    var nestedFrames = viewItems
//                        .ToObservable()
//                        .ToNestedFrames(nestedObjectTypes);
                    var objectChanged = detailView.WhenCurrentObjectChanged()
//                        .FirstAsync()
                        .SelectMany(tuple => viewItems.ToNestedFrames(nestedObjectTypes));
//                        .Switch();
                    return objectChanged.Select(_ =>
                        (detailView, _.nestedFrame));
                });
        }


        public static IObservable<(DetailView detailView, NestedFrame nestedFrame)>
            WhenChildrenCurrentObjectChanged(this IObservable<DetailView> source, params Type[] nestedObjectTypes){

            return source.WhenChildrenCurrentObjectChanged<object>(nestedObjectTypes);
        }

    }
}