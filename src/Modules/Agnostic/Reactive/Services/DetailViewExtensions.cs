using System;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.XAF.Modules.Reactive.Services;

namespace DevExpress.XAF.Modules.Reactive.Services{
    public static class DetailViewExtensions{
        public static IObservable<Frame> FrameAssigned(this IObservable<DetailView> source){
            var rootFrames = RxApp.FrameAssignedToController.ViewChanged().Where(tuple => tuple.frame.View.IsRoot).Select(tuple => tuple.frame);
            return source.OfType<DetailView>().WhenChildrenCurrentObjectChanged<object>().Select(tuple => tuple.nestedFrame).Cast<Frame>().Concat(rootFrames );
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
                        .SelectMany(tuple => { return viewItems.ToNestedFrames(nestedObjectTypes); });
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