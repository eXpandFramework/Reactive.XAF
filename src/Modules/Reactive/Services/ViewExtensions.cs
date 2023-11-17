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
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.ViewExtensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ViewExtensions{
        private static readonly Type GridListEditorType;

        static ViewExtensions() {
            GridListEditorType = AppDomain.CurrentDomain.GetAssemblyType("DevExpress.ExpressApp.Win.Editors.GridListEditor");
        }

        public static IObservable<object> WhenObjects(this ListView listView) 
            => listView.Objects().ToNowObservable()
                .MergeToObject(listView.CollectionSource.WhenCollectionChanged()
                    .SelectMany(_ => listView.Objects()))
                .MergeToObject(listView.CollectionSource.WhenCriteriaApplied().SelectMany(@base => @base.Objects() ))
                .MergeToObject(listView.Editor.WhenEvent(nameof(listView.Editor.DataSourceChanged)).To(listView.Editor.DataSource)
                    .StartWith(listView.Editor.DataSource).WhenNotDefault()
                    .Select(datasource => ((IEnumerable)datasource).Cast<object>()));
        
        public static IObservable<object> SelectObject(this ListView listView, params object[] objects)
            => listView.SelectObject<object>(objects);
        
        public static IObservable<TO> SelectObject<TO>(this ListView listView,params TO[] objects) where TO : class 
            => listView.Editor.WhenControlsCreated()
                .SelectMany(editor => editor.Control.WhenEvent("DataSourceChanged")).To(listView)
                .SelectObject(objects);
        
        static IObservable<T> SelectObject<T>(this IObservable<ListView> source,params T[] objects) where T : class 
            => source.SelectMany(view => {
                if (view.Editor.GetType().InheritsFrom(GridListEditorType)){
                    var gridView = view.Editor.GetPropertyValue("GridView");
                    return objects.Select(obj => {
                            gridView.CallMethod("ClearSelection");
                            var index = (int)gridView.CallMethod("FindRow", obj);
                            gridView.CallMethod("SelectRow", index);
                            return index;
                        }).ToNowObservable()
                        .BufferUntilCompleted()
                        .Select(indexes => {
                            if (indexes.Length==1){
                                gridView.SetPropertyValue("FocusedRowHandle", indexes.First()) ;
                            }
                            return gridView.GetPropertyValue("FocusedRowObject") as T;
                        });
                }
                throw new NotImplementedException(nameof(view.Editor));
            });
        
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
        public static IObservable<object> WhenObjects(this View view) 
            => view is ListView listView?listView.CollectionSource.WhenCollectionChanged()
                .MergeToUnit(listView.CollectionSource.WhenCriteriaApplied().Select(@base => @base)).SelectMany(_ => listView.Objects())
                .StartWith(listView.Objects()):view.ToDetailView().WhenCurrentObjectChanged()
                .Select(detailView => detailView.CurrentObject).StartWith(view.CurrentObject).WhenNotDefault();
        
        public static IObservable<T> WhenObjects<T>(this View view) 
            => view.WhenObjects().Cast<T>();
        
        public static IObservable<T> WhenActivated<T>(this T view) where T : View 
            => view.WhenViewEvent(nameof(View.Activated));

        public static IObservable<T> Activated<T>(this IObservable<T> source) where T:View 
            => source.SelectMany(view => view.WhenActivated());

        public static IObservable<T> WhenClosed<T>(this T view) where T : View 
            => view.WhenViewEvent(nameof(view.Closed));

        public static IObservable<(T view, CancelEventArgs e)> WhenQueryCanClose<T>(this T view) where T : View 
            => view.WhenViewEvent<T,CancelEventArgs>(nameof(View.QueryCanClose));

        public static IObservable<(T view, CancelEventArgs e)> QueryCanClose<T>(this IObservable<T> source) where T:View 
            => source.Cast<T>().SelectMany(view => view.WhenQueryCanClose());

        public static IObservable<(T view, CancelEventArgs e)> WhenQueryCanChangeCurrentObject<T>(this T view) where T : View 
            => view.WhenViewEvent<T,CancelEventArgs>(nameof(View.QueryCanChangeCurrentObject));

        public static IObservable<(T view, CancelEventArgs e)> QueryCanChangeCurrentObject<T>(this IObservable<T> source) where T:View 
            => source.Cast<T>().SelectMany(view => view.WhenQueryCanChangeCurrentObject());

        public static IObservable<T> WhenControlsCreated<T>(this T view,bool emitExisting=false) where T : View 
            =>emitExisting&&view.IsControlCreated?view.Observe(): view.WhenViewEvent(nameof(View.ControlsCreated));

        public static IObservable<T> WhenControlsCreated<T>(this IObservable<T> source) where T:View 
            => source.SelectMany(view => view.WhenViewEvent(nameof(View.ControlsCreated)));

        public static IObservable<T> WhenSelectionChanged<T>(this T view, int waitUntilInactiveSeconds = 0) where T : View
            => view.WhenViewEvent(nameof(View.SelectionChanged)).To(view)
                .Publish(changed => waitUntilInactiveSeconds > 0 ? changed.WaitUntilInactive(waitUntilInactiveSeconds) : changed);
        
        public static IObservable<T[]> SelectedObjects<T>(this IObservable<ObjectView> source,ObjectView objectView=null) 
            => source.Select(view => view.SelectedObjects.Cast<T>().ToArray()).StartWith(objectView!=null?objectView.SelectedObjects.Cast<T>().ToArray():Array.Empty<T>());

        public static IObservable<T> SelectionChanged<T>(this IObservable<T> source,int waitUntilInactiveSeconds=0) where T:View 
            => source.SelectMany(item => item.WhenSelectionChanged()).Cast<T>()
                .Publish(changed => waitUntilInactiveSeconds > 0 ? source.WaitUntilInactive(waitUntilInactiveSeconds) : changed);

        public static IObservable<T> CurrentObjectChanged<T>(this IObservable<T> source) where T:View 
            => source.SelectMany(item => item.WhenCurrentObjectChanged());

        public static IObservable<object> WhenSelectedObjects(this View view) 
            => view.WhenSelectionChanged().SelectMany(_ => view.SelectedObjects.Cast<object>())
                .StartWith(view.SelectedObjects.Cast<object>());
        
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
        
        public static IObservable<TView> When<TView>(this IObservable<TView> source,string viewId) where TView:View 
            => source.Where(view =>view.Id==viewId);

        public static IObservable<TSource[]> RefreshObjectSpace<TSource>(this IObservable<TSource> source,View view) 
            => source.BufferUntilCompleted().ObserveOnContext().Do(_ => view.ObjectSpace.Refresh());


    }
}