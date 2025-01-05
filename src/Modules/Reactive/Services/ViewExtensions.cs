using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Validation;
using Fasterflect;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ViewExtensions{
        private static readonly Type GridListEditorType;

        static ViewExtensions() => GridListEditorType = AppDomain.CurrentDomain.GetAssemblyType("DevExpress.ExpressApp.Win.Editors.GridListEditor");

        public static IObservable<Frame> WhenFrame(this CompositeView view)
            => view.Application().WhenFrame(view.Id);
        
        public static IObservable<object> SelectObject(this ListView listView, params object[] objects)
            => listView.SelectObject<object>(objects);
        
        public static IObservable<TO> SelectObject<TO>(this ListView listView,params TO[] objects) where TO : class 
            => listView.Application().GetPlatform() == Platform.Blazor
                ? listView.Application().GetRequiredService<IObjectSelector<TO>>().SelectObject(listView, objects)
                : listView.Editor.WhenControlsCreated()
                    .SelectMany(editor => editor.WhenEvent("DataSourceChanged").To(listView).StartWith(listView)
                        .WhenNotDefault(_ => editor.GetPropertyValue("DataSource"))
                        .WhenNotDefault(_ => editor.List.Count)
                    )
                    .To(listView)
                    .SelectObject(objects)
                    .Take(objects.Length);

        
        static IObservable<T> SelectObject<T>(this IObservable<ListView> source,params T[] objects) where T : class 
            => source.SelectMany(view => {
                if (!view.Editor.GetType().InheritsFrom(GridListEditorType))
                    throw new NotImplementedException(nameof(view.Editor));
                var gridView = view.Editor.GetPropertyValue("GridView");
                gridView.CallMethod("Focus");
                var focus = gridView.GetPropertyValue("FocusedRowHandle");
                return objects.ToNowObservable().SelectManySequential(arg => gridView.WhenSelectRow(arg)
                    .BufferUntilCompleted().SelectMany(handles => {
                        if (handles.First() == (int)focus) return handles;
                        gridView.CallMethod("UnselectRow", focus);
                        return handles;
                    }).To(arg));
            });

        public static IObservable<CreateCustomCurrentObjectDetailViewEventArgs> WhenCreateCustomCurrentObjectDetailView(this ListView listView) 
            => listView.WhenViewEvent<ListView, CreateCustomCurrentObjectDetailViewEventArgs>(nameof(ListView.CreateCustomCurrentObjectDetailView)).ToSecond();

        static IObservable<int> WhenSelectRow<T>(this object gridView, T row) where T : class 
            => gridView.Defer(() => {
                var rowHandle = gridView.RowHandle( row);
                if ((int)gridView.GetPropertyValue("FocusedRowHandle") == rowHandle) {
                    gridView.CallMethod("SelectRow", rowHandle);
                    return rowHandle.Observe().Where(i =>i>-1 );
                }
                return Observable.While(() => {
                        gridView.CallMethod("MakeRowVisible",rowHandle);
                        gridView.SetPropertyValue("FocusedRowHandle", rowHandle);
                        return gridView.CallMethod("IsRowVisible", rowHandle).ToString() == "Hidden";
                    }, Observable.Timer(1.Seconds()).Select(l => l.Change<int>()).ObserveOnContext().IgnoreElements())
                    .ConcatDefer(() => rowHandle.Observe().Do(_ => {
                        gridView.CallMethod("SelectRow", rowHandle);
                        gridView.SetPropertyValue("FocusedRowHandle", rowHandle);
                    }));
            });

        private static int RowHandle<T>(this object gridView, T row) where T : class => (int)gridView.CallMethod("FindRow",row);

        public static IObservable<CustomizeShowViewParametersEventArgs> WhenNestedListViewProcessCustomizeShowViewParameters(
            this DetailView detailView, params Type[] objectTypes) 
            => detailView.FrameContainers(objectTypes).ToNowObservable()
                .SelectMany(frame => frame.GetController<ListViewProcessCurrentObjectController>()
                    .WhenEvent(nameof(ListViewProcessCurrentObjectController.CustomizeShowViewParameters))
                    .Select(pattern => pattern.EventArgs).Cast<CustomizeShowViewParametersEventArgs>());

        public static IObservable<T> WhenClosing<T>(this T view) where T : View 
            => view.WhenViewEvent(nameof(view.Closing)).To(view).Select(view1 => view1);

        public static IEnumerable<PropertyEditor> CloneRequiredMembers(this CompositeView compositeView,object existingObject=null) {
            existingObject ??= compositeView.ObjectSpace.FindObject(compositeView.ObjectTypeInfo.Type);
            return compositeView.GetItems<PropertyEditor>().Where(editor =>
                    editor.MemberInfo.FindAttributes<RuleRequiredFieldAttribute>().Any())
                .Do(editor => { editor.MemberInfo.SetValue(compositeView.CurrentObject, editor.MemberInfo.GetValue(existingObject)); });
        }
        public static IObservable<object[]> WhenObjects(this View view)
            => view is ListView listView ? listView.CollectionSource.WhenCollectionChanged().Select(@base => @base)
                    .MergeToUnit(listView.ObjectSpace.WhenReloaded())
                    .MergeToUnit(listView.Editor.WhenDatasourceChanged())
                    .MergeToUnit(listView.CollectionSource.WhenCriteriaApplied())
                    .Select(_ => listView.Objects().ToArray())
                    .StartWith([listView.Objects().ToArray()])
                : view.ToDetailView().WhenCurrentObjectChanged()
                    .Select(detailView => new[] { detailView.CurrentObject })
                    .StartWith([[view.CurrentObject]]);

        public static IObservable<T[]> WhenObjects<T>(this View view) 
            => view.WhenObjects().Select(objects => objects.Cast<T>().ToArray());
        
        public static IObservable<T> WhenActivated<T>(this T view) where T : View 
            => view.WhenViewEvent(nameof(View.Activated));

        public static IObservable<T> Activated<T>(this IObservable<T> source) where T:View 
            => source.SelectMany(view => view.WhenActivated());

        public static IObservable<T> WhenClosed<T>(this T view) where T : View 
            => view.WhenViewEvent(nameof(view.Closed));

        public static IObservable<(T view, CancelEventArgs e)> WhenQueryCanClose<T>(this T view) where T : View 
            => view.WhenViewEvent<T,CancelEventArgs>(nameof(View.QueryCanClose));
        
        public static IObservable<(T view, ViewItemsChangedEventArgs e)> WhenItemsChanged<T>(this T view,bool emitExisting=false,params ViewItemsChangedType[] changedTypes) where T : View 
            => view.WhenViewEvent<T, ViewItemsChangedEventArgs>(nameof(CompositeView.ItemsChanged))
                .StartWith(() => emitExisting,view.ToCompositeView().GetItems<ViewItem>().Select(item => (view,new ViewItemsChangedEventArgs((ViewItemsChangedType)(-1),item))).ToArray())
                .Where(t => !changedTypes.Any()||changedTypes.Contains(t.Item2.ChangedType));

        public static IObservable<(T view, CancelEventArgs e)> QueryCanClose<T>(this IObservable<T> source) where T:View 
            => source.Cast<T>().SelectMany(view => view.WhenQueryCanClose());

        public static IObservable<(T view, CancelEventArgs e)> WhenQueryCanChangeCurrentObject<T>(this T view) where T : View 
            => view.WhenViewEvent<T,CancelEventArgs>(nameof(View.QueryCanChangeCurrentObject));

        public static IObservable<(T view, CancelEventArgs e)> QueryCanChangeCurrentObject<T>(this IObservable<T> source) where T:View 
            => source.Cast<T>().SelectMany(view => view.WhenQueryCanChangeCurrentObject());

        public static IObservable<T> WhenControlsCreated<T>(this T view,bool emitExisting=false) where T : View 
            =>emitExisting&&view.IsControlCreated?view.Observe(): view.WhenViewEvent(nameof(View.ControlsCreated));

        public static IObservable<T> WhenControlsCreated<T>(this IObservable<T> source,bool emitExisting=false) where T:View 
            => source.SelectMany(view => view.WhenControlsCreated(emitExisting));

        public static IObservable<View> WhenSelectedObjectsChanged(this View view) 
            => view.WhenSelectionChanged().Select(view1 => view1.SelectedObjects.Cast<object>().ToArray()).CombineWithPrevious()
                .Where(t => {
                    if (t.previous == null) return true;
                    var currentObjects = t.current.Select(o => view.ObjectSpace.GetKeyValue(o)).ToArray();
                    return t.previous.Select(o => view.ObjectSpace.GetKeyValue(o))
                        .Any(o => !currentObjects.Contains(o));
                })
                .To(view);

        public static IObservable<T> WhenSelectionChanged<T>(this T view, int waitUntilInactiveSeconds = 0) where T : View
            => view.WhenViewEvent(nameof(View.SelectionChanged)).To(view)
                .Publish(changed => waitUntilInactiveSeconds > 0 ? changed.WaitUntilInactive(waitUntilInactiveSeconds) : changed);
        
        public static IObservable<T[]> SelectedObjects<T>(this IObservable<ObjectView> source,ObjectView objectView=null) 
            => source.Select(view => view.SelectedObjects.Cast<T>().ToArray()).StartWith(objectView!=null?objectView.SelectedObjects.Cast<T>().ToArray():[]);

        public static IObservable<T> SelectionChanged<T>(this IObservable<T> source,int waitUntilInactiveSeconds=0) where T:View 
            => source.SelectMany(item => item.WhenSelectionChanged()).Cast<T>()
                .Publish(changed => waitUntilInactiveSeconds > 0 ? source.WaitUntilInactive(waitUntilInactiveSeconds) : changed);

        public static IObservable<T> CurrentObjectChanged<T>(this IObservable<T> source) where T:View 
            => source.SelectMany(item => item.WhenCurrentObjectChanged());

        public static IObservable<IList> WhenSelectedObjects(this View view) 
            => view.WhenSelectionChanged().Select(_ => view.SelectedObjects)
                .StartWith(view.SelectedObjects);
        public static IObservable<T[]> WhenSelectedObjects<T>(this View view) 
            => view.WhenSelectedObjects().Select(list => list.OfType<T>().ToArray());
        
        public static IObservable<Frame> ToFrame(this IObservable<DashboardViewItem> source)
            => source.Select(item => item.Frame);
        
        public static (ITypeInfo ObjectTypeInfo, object) CurrentObjectInfo(this View view) 
            => (view.ObjectTypeInfo,view.ObjectSpace.GetKeyValue(view.CurrentObject));
        
        public static IObservable<Frame> ToEditFrame(this IObservable<ListView> source) 
            => source.Select(view => view.EditFrame);
        public static IObservable<T> WhenCurrentObjectChanged<T>(this T view) where T:View 
            => view.WhenViewEvent(nameof(View.CurrentObjectChanged)).To(view);

        public static IObservable<TView> TakeUntilViewDisposed<TView>(this IObservable<TView> source) where TView:View 
            => source.TakeWhileInclusive(view => !view.IsDisposed);

        public static IObservable<TView> WhenViewEvent<TView>(this TView view,string eventName) where TView:View 
            => view.WhenEvent(eventName).To(view).TakeUntilViewDisposed();
        
        public static IObservable<(TView value, TArgs source)> WhenViewEvent<TView,TArgs>(this TView view,string eventName) where TView:View where TArgs : EventArgs 
            => view.WhenEvent<TArgs>(eventName).InversePair(view).TakeUntilViewDisposed();
        
        public static IObservable<(TView view, TArgs e)> TakeUntilViewDisposed<TView,TArgs>(this IObservable<(TView view,TArgs)> source) where TView:View where TArgs:EventArgs 
            => source.TakeWhileInclusive(t => !t.view.IsDisposed);
        
        public static IObservable<Unit> WhenCustomizeViewShortcut<TView>(this TView view) where TView:View 
            => view.WhenViewEvent(nameof(View.CustomizeViewShortcut)).ToUnit();

        public static IObservable<ListPropertyEditor> NestedListViews<TView>(this IObservable<TView> views, params Type[] objectTypes) where TView : DetailView 
            => views.SelectMany(detailView => detailView.NestedListViews(objectTypes));

        public static IObservable<ListPropertyEditor> NestedListViews<TView>(this TView view, params Type[] objectTypes ) where TView : DetailView 
            => view.NestedViewItems<TView,ListPropertyEditor>(objectTypes);
        
        public static IObservable<TViewItem> NestedViewItems<TView,TViewItem>(this TView view, params Type[] objectTypes ) where TView : DetailView where TViewItem:ViewItem,IFrameContainer 
            => view.NestedFrameContainers(objectTypes).OfType<TViewItem>();
        public static IObservable<IFrameContainer> NestedFrameContainers<TView>(this TView view, params Type[] objectTypes ) where TView : CompositeView  
            => view.GetItems<IFrameContainer>().ToNowObservable().OfType<ViewItem>()
                .SelectMany(item => item.WhenControlCreated().StartWith(item.Control).WhenNotDefault().Take(1).To(item).Cast<IFrameContainer>())
                .NestedFrameContainers(view, objectTypes);
        
        public static IObservable<DashboardViewItem> NestedDashboards<TView>(this TView view, params Type[] objectTypes ) where TView : DetailView 
            => view.NestedViewItems<TView,DashboardViewItem>( objectTypes);

        private static IObservable<TFrameContainer> NestedFrameContainers<TView,TFrameContainer>(this IObservable<TFrameContainer> lazyListPropertyEditors, TView view, Type[] objectTypes) where TView : CompositeView where TFrameContainer:IFrameContainer{
            var listFrameContainers = view.GetItems<ViewItem>().OfType<TFrameContainer>().Where(editor => editor.Frame?.View != null)
                .ToNowObservable().Merge(lazyListPropertyEditors);
            var nestedEditors = lazyListPropertyEditors.WhenNotDefault(container => container.Frame).SelectMany(frameContainer => {
                var detailView =frameContainer.Frame.View is ListView listView? listView.EditView:null;
                return detailView != null ? detailView.NestedFrameContainers(objectTypes).OfType<TFrameContainer>() : Observable.Never<TFrameContainer>();
            });
            return listFrameContainers.WhenNotDefault(container => container.Frame)
                .Where(frameContainer =>!objectTypes.Any()|| objectTypes.Any(type => type.IsAssignableFrom(frameContainer.Frame.View.ObjectTypeInfo.Type)))
                .Merge(nestedEditors).Distinct()
                .Select(container => container);
        }

        
        public static IObservable<TView> When<TView>(this IObservable<TView> source,Type objectType=null,ViewType viewType=ViewType.Any,Nesting nesting=Nesting.Any) where TView:View 
            => source.Where(view =>view.Is(viewType,nesting,objectType ?? typeof(object)));
        
        public static IObservable<TView> When<TView>(this IObservable<TView> source,string viewId) where TView:View 
            => source.Where(view =>view.Id==viewId);

        public static IObservable<TSource[]> RefreshObjectSpace<TSource>(this IObservable<TSource> source,View view) 
            => source.BufferUntilCompleted().ObserveOnContext().Do(_ => view.ObjectSpace?.Refresh());


    }
}