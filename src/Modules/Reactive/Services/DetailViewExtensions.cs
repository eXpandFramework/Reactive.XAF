using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class DetailViewExtensions{
        public static IObservable<(DetailView detailView, CancelEventArgs e)> WhenViewEditModeChanging(this DetailView detailView) 
            => detailView.WhenEvent<CancelEventArgs>(nameof(DetailView.ViewEditModeChanging)).InversePair(detailView);

        public static IObservable<(DetailView detailView, CancelEventArgs e)> ViewEditModeChanging<T>(this IObservable<T> source) where T : DetailView 
            => source.SelectMany(_ => _.WhenViewEditModeChanging());

        public static IObservable<(DetailView detailView, NestedFrame nestedFrame)> WhenChildren<TParentObject>(this IObservable<DetailView> source, params Type[] nestedObjectTypes) 
            => source.Where(view => typeof(TParentObject).IsAssignableFrom(view.ObjectTypeInfo.Type))
                .SelectMany(detailView => {
                    var viewItems = detailView.GetItems<ViewItem>().OfType<IFrameContainer>().Cast<ViewItem>().ToObservable();
                    return detailView.IsRoot ? viewItems.ControlCreated().ToNestedFrames(nestedObjectTypes).Select(_ => (detailView, _.nestedFrame))
                        : viewItems.ToNestedFrames(nestedObjectTypes).Select(_ => (detailView, _.nestedFrame));
                });

        public static IObservable<(DetailView detailView, NestedFrame nestedFrame)> WhenChildrenCurrentObjectChanged<TParentObject>(this IObservable<DetailView> source,params Type[] nestedObjectTypes) 
            => source.Where(view => typeof(TParentObject).IsAssignableFrom(view.ObjectTypeInfo.Type))
                .SelectMany(detailView => {
                    var viewItems = detailView.GetItems<ViewItem>().OfType<IFrameContainer>().Cast<ViewItem>().ToObservable();
                    return detailView.IsRoot ? viewItems.ControlCreated().ToNestedFrames(nestedObjectTypes).Select(_ => (detailView, _.nestedFrame))
                        : detailView.WhenCurrentObjectChanged().SelectMany(_ => viewItems.ToNestedFrames(nestedObjectTypes))
                            .Select(_ => (detailView, _.nestedFrame));
                });


        public static IObservable<(DetailView detailView, NestedFrame nestedFrame)>
            WhenChildrenCurrentObjectChanged(this IObservable<DetailView> source, params Type[] nestedObjectTypes) 
            => source.WhenChildrenCurrentObjectChanged<object>(nestedObjectTypes);
    }
}