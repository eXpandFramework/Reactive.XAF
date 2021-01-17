using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Fasterflect;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.ModelExtensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class PropertyEditorService{
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
                .Do(_ => _.editor.SetParentView(_.view)).ToUnit();
            return setupParentView;
        }

        public static IObservable<T> WhenVisibilityChanged<T>(this T editor) where T:PropertyEditor 
            => Observable.FromEventPattern<EventHandler, EventArgs>(h => editor.VisibilityChanged += h,
                    h => editor.VisibilityChanged -= h, Scheduler.Immediate)
                .Select(pattern => pattern.Sender).Cast<T>();

        public static IObservable<T> WhenVisibilityChanged<T>(this IObservable<T> source) where T:PropertyEditor 
            => source.SelectMany(editor => editor.WhenVisibilityChanged());
    }

    public interface IParentViewPropertyEditor{
        void SetParentView(ObjectView value);
    }

}