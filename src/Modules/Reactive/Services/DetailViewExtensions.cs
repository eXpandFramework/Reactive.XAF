using System;
using System.ComponentModel;
using System.Linq;
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
        public static IObservable<Frame> FrameAssigned(this IObservable<DetailView> source){
            throw new NotImplementedException();   
//            var rootFrames = RxApp.FrameAssignedToController.ViewChanged().Where(tuple => tuple.frame.View.IsRoot).Select(tuple => tuple.frame);
//            return source.OfType<DetailView>().WhenChildrenCurrentObjectChanged<object>().Select(tuple => tuple.nestedFrame).Cast<Frame>().Concat(rootFrames );
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