using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
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
            => view.Observe().WhenNotDefault().Closing();
        
        public static IObservable<object> WhenObjects(this View view) 
            => view is ListView listView?listView.CollectionSource.WhenCollectionChanged().SelectMany(_ => listView.Objects())
                .StartWith(listView.Objects()):view.ToDetailView().WhenCurrentObjectChanged()
                .Select(detailView => detailView.CurrentObject).StartWith(view.CurrentObject).WhenNotDefault();
        
        public static IObservable<T> WhenObjects<T>(this View view) 
            => view.WhenObjects().Cast<T>();

        public static IObservable<T> Closing<T>(this IObservable<T> source) where T:View 
            => source.Cast<View>().SelectMany(view => view.WhenEvent(nameof(View.Closing)))
                .Select(pattern => (T)pattern.Sender);

        public static IObservable<T> WhenActivated<T>(this T view) where T : View 
            => view.WhenEvent(nameof(View.Activated)).Select(pattern => (T)pattern.Sender);

        public static IObservable<T> Activated<T>(this IObservable<T> source) where T:View 
            => source.SelectMany(view => view.WhenActivated());
        
        public static IObservable<T> WhenModelChanged<T>(this T view) where T : View 
            => view.Observe().ModelChanged();

        public static IObservable<T> ModelChanged<T>(this IObservable<T> source) where T:View 
            => source.Cast<View>().SelectMany(view => view.WhenEvent(nameof(View.ModelChanged)))
                .Select(pattern => (T)pattern.Sender);

        public static IObservable<T> WhenClosed<T>(this T view) where T : View 
            => view == null ? Observable.Empty<T>() : view.Observe().Closed();

        public static IObservable<T> Closed<T>(this IObservable<T> source) where T:View 
            => source.SelectMany(view => view.WhenEvent(nameof(View.Closed)))
                .Select(pattern => (T)pattern.Sender);

        public static IObservable<(T view, CancelEventArgs e)> WhenQueryCanClose<T>(this T view) where T : View 
            => view.Observe().QueryCanClose();

        public static IObservable<(T view, CancelEventArgs e)> QueryCanClose<T>(this IObservable<T> source) where T:View 
            => source.Cast<T>().SelectMany(view => view.WhenEvent<CancelEventArgs>(nameof(View.QueryCanClose))
                .InversePair(view));

        public static IObservable<(T view, CancelEventArgs e)> WhenQueryCanChangeCurrentObject<T>(this T view) where T : View 
            => view.WhenEvent<CancelEventArgs>(nameof(View.QueryCanChangeCurrentObject)).InversePair(view);

        public static IObservable<(T view, CancelEventArgs e)> QueryCanChangeCurrentObject<T>(this IObservable<T> source) where T:View 
            => source.Cast<T>().SelectMany(view => view.WhenQueryCanChangeCurrentObject());

        public static IObservable<T> WhenControlsCreated<T>(this T view) where T : View 
            => view.WhenEvent(nameof(View.ControlsCreated)).To(view);

        public static IObservable<T> WhenControlsCreated<T>(this IObservable<T> source) where T:View 
            => source.SelectMany(view => view.WhenEvent(nameof(View.ControlsCreated))
                .Select(pattern => pattern.Sender).Cast<T>());

        public static IObservable<T> WhenSelectionChanged<T>(this T view, int waitUntilInactiveSeconds = 0) where T : View
            => view.WhenEvent(nameof(View.SelectionChanged)).To(view)
                .Publish(changed => waitUntilInactiveSeconds > 0 ? changed.WaitUntilInactive(waitUntilInactiveSeconds) : changed);
        
        public static IObservable<T[]> SelectedObjects<T>(this IObservable<ObjectView> source,ObjectView objectView=null) 
            => source.Select(view => view.SelectedObjects.Cast<T>().ToArray()).StartWith(objectView!=null?objectView.SelectedObjects.Cast<T>().ToArray():Array.Empty<T>());

        public static IObservable<T> SelectionChanged<T>(this IObservable<T> source,int waitUntilInactiveSeconds=0) where T:View 
            => source.SelectMany(item => item.WhenSelectionChanged()).Cast<T>()
                .Publish(changed => waitUntilInactiveSeconds > 0 ? source.WaitUntilInactive(waitUntilInactiveSeconds) : changed);

        public static IObservable<T> CurrentObjectChanged<T>(this IObservable<T> source) where T:View 
            => source.SelectMany(item => item.WhenCurrentObjectChanged());

        public static IObservable<T> WhenCurrentObjectChanged<T>(this T view) where T:View 
            => view.WhenEvent(nameof(View.CurrentObjectChanged)).To(view);

        public static IObservable<Unit> WhenDisposingView<TView>(this TView view) where TView:View 
            => view.WhenEvent(nameof(view.Disposing)).ToUnit();

        public static IObservable<Unit> Disposing<TView>(this IObservable<TView> source) where TView:View 
            => source.SelectMany(item => item.WhenDisposingView()).ToUnit();

        public static IObservable<ListPropertyEditor> NestedListViews<TView>(this IObservable<TView> views, params Type[] objectTypes) where TView : DetailView 
            => views.WhenControlsCreated().SelectMany(detailView => detailView.NestedListViews(objectTypes));

        public static IObservable<ListPropertyEditor> NestedListViews<TView>(this TView view, params Type[] objectTypes ) where TView : DetailView 
            => view.NestedViewItems<TView,ListPropertyEditor>(objectTypes);
        
        public static IObservable<TViewItem> NestedViewItems<TView,TViewItem>(this TView view, params Type[] objectTypes ) where TView : DetailView where TViewItem:ViewItem,IFrameContainer 
            => view.NestedFrameContainers(objectTypes).OfType<TViewItem>();
        
        public static IObservable<IFrameContainer> NestedFrameContainers<TView>(this TView view, params Type[] objectTypes ) where TView : DetailView  
            => view.GetItems<IFrameContainer>().Where(editor =>editor.Frame?.View == null).ToNowObservable()
                .SelectMany(frameContainer =>frameContainer.Cast<ViewItem>().Control==null? frameContainer.Cast<ViewItem>().WhenControlCreated().To(frameContainer):frameContainer.Observe())
                .NestedFrameContainers(view, objectTypes);
        
        public static IObservable<DashboardViewItem> NestedDashboards<TView>(this TView view, params Type[] objectTypes ) where TView : DetailView 
            => view.NestedViewItems<TView,DashboardViewItem>( objectTypes);

        private static IObservable<TFrameContainer> NestedFrameContainers<TView,TFrameContainer>(this IObservable<TFrameContainer> lazyListPropertyEditors, TView view, Type[] objectTypes) where TView : DetailView where TFrameContainer:IFrameContainer{
            var listFrameContainers = view.GetItems<ViewItem>().OfType<TFrameContainer>().Where(editor => editor.Frame?.View != null)
                .ToNowObservable().Merge(lazyListPropertyEditors);

            var nestedEditors = listFrameContainers.WhenNotDefault(container => container.Frame).SelectMany(frameContainer => {
                var detailView =frameContainer.Frame.View is ListView listView? listView.EditView:null;
                return detailView != null ? detailView.NestedFrameContainers(objectTypes).OfType<TFrameContainer>() : Observable.Never<TFrameContainer>();
            });
            return listFrameContainers.WhenNotDefault(container => container.Frame)
                .Where(frameContainer =>!objectTypes.Any()|| objectTypes.Any(type => type.IsAssignableFrom(frameContainer.Frame.View.ObjectTypeInfo.Type)))
                .Merge(nestedEditors);
        }

        public static IObservable<TView> When<TView>(this IObservable<TView> source,Type objectType=null,ViewType viewType=ViewType.Any,Nesting nesting=Nesting.Any) where TView:View 
            => source.Where(view =>view.Is(viewType,nesting,objectType ?? typeof(object)));

        public static IObservable<TSource[]> RefreshObjectSpace<TSource>(this IObservable<TSource> source,View view) 
            => source.BufferUntilCompleted().ObserveOnContext().Do(_ => view.ObjectSpace.Refresh());


    }
}