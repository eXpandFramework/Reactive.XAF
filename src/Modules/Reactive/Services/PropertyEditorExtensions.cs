using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using Fasterflect;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.FaultHub;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class PropertyEditorExtensions{
        static readonly Type PopupWindowShowActionHelperType = AppDomain.CurrentDomain.GetAssemblyType("DevExpress.ExpressApp.Win.PopupWindowShowActionHelper");

        #region High-Level Logical Operations
        public static IObservable<ListPropertyEditor> FrameChanged(this IEnumerable<ListPropertyEditor> source) 
            => source.ToObservable().SelectMany(editor => WhenFrameChanged(editor).Select(_ => editor)).PushStackFrame();

        public static IObservable<Window> ShowPopupWindow(this PopupWindowShowAction action) 
            => action.Application.WhenFrame().OfType<Window>().Merge(action.DeferAction(()
                => PopupWindowShowActionHelperType.CreateInstance(action).CallMethod("ShowPopupWindow")).To<Window>()).PushStackFrame();

        internal static IObservable<Unit> SetupPropertyEditorParentView(this XafApplication application){
            var detailViewEditors = application.WhenDetailViewCreated().ToDetailView()
                .SelectMany(detailView => detailView.GetItems<IParentViewPropertyEditor>().Select(editor => (view: (ObjectView) detailView, editor)));
            var listViewEditors = application.WhenListViewCreated()
                .SelectMany(listView => listView.WhenControlsCreated())
                .SelectMany(listView => listView.Editor.GetType().InheritsFrom("DevExpress.ExpressApp.Web.Editors.ComplexWebListEditor")
                    ? listView.Model.MemberViewItems(typeof(IParentViewPropertyEditor))
                        .Select(item => listView.Editor.CallMethod("FindPropertyEditor", item, ViewEditMode.Edit) as IParentViewPropertyEditor)
                        .ToObservable().WhenNotDefault()
                        .Select(editor => (view: (ObjectView) listView, editor))
                    : Observable.Empty<(ObjectView view, IParentViewPropertyEditor editor)>());

            var setupParentView = detailViewEditors.Merge(listViewEditors)
                .Do(t => t.editor.SetParentView(t.view)).ToUnit();
            return setupParentView.PushStackFrame();
        }
        #endregion

        #region Low-Level Plumbing
        public static bool IsDisposed(this PropertyEditor editor)
            => (bool)editor.GetPropertyValue("IsDisposed");

        public static IObservable<ListPropertyEditor> WhenFrameChanged(this ListPropertyEditor editor) 
            => editor.ProcessEvent(nameof(ListPropertyEditor.FrameChanged)).To(editor).TakeUntilDisposed();
        
        public static object DisplayableMemberValue(this PropertyEditor editor,object currentObject=null,object propertyValue=null) {
            currentObject ??= editor.CurrentObject;
            propertyValue ??= editor.PropertyValue;
            var defaultMember = editor.MemberInfo.FindDisplayableMember();
            return defaultMember != null ? defaultMember.GetValue(currentObject) : propertyValue;
        }
            
        public static IObservable<T> WhenVisibilityChanged<T>(this T editor) where T:PropertyEditor 
            => editor.ProcessEvent(nameof(PropertyEditor.VisibilityChanged)).TakeUntil(_ => editor.IsDisposed()).To(editor);

        public static IObservable<T> WhenVisibilityChanged<T>(this IObservable<T> source) where T:PropertyEditor 
            => source.SelectMany(editor => editor.WhenVisibilityChanged());
        #endregion
    }

    public interface IParentViewPropertyEditor{
        void SetParentView(ObjectView value);
    }
}