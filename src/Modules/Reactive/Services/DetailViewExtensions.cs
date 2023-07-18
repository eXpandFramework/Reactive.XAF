using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class DetailViewExtensions{
        public static IObservable<(DetailView detailView, CancelEventArgs e)> WhenViewEditModeChanging(this DetailView detailView) 
            => detailView.WhenViewEvent<DetailView,CancelEventArgs>(nameof(DetailView.ViewEditModeChanging));

        public static IObservable<(DetailView detailView, CancelEventArgs e)> ViewEditModeChanging<T>(this IObservable<T> source) where T : DetailView 
            => source.SelectMany(view => view.WhenViewEditModeChanging());

        public static IObservable<DetailView> SetDefaultFocusedItem(this IObservable<DetailView> source,string viewItemId)
            => source.Do(view => view.SetDefaultFocusedItem(viewItemId));

        public static void SetDefaultFocusedItem(this DetailView view, string viewItemId) 
            => view.Model.Cast<IModelDetailViewDefaultFocusedItem>().DefaultFocusedItem =
                view.Model.Items.First(viewItem => viewItem.Id == viewItemId);

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