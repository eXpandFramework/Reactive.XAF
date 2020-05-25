using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Fasterflect;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Type;
using Xpand.Extensions.XAF.Model;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class PropertyEditorService{
        internal static IObservable<Unit> SetupPropertyEditorParentView(this XafApplication application){
            var detailViewEditors = application.WhenDetailViewCreated().ToDetailView()
                .SelectMany(detailView => detailView.GetItems<IParentViewPropertyEditor>().Select(editor => (view: (ObjectView) detailView, editor)));
            var listViewEditors = application.WhenListViewCreated()
                .SelectMany(listView => listView.WhenControlsCreated())
                .SelectMany(listView => {
                    if (listView.Editor.GetType().InheritsFrom("DevExpress.ExpressApp.Web.Editors.ComplexWebListEditor")){
                        return listView.Model.MemberViewItems(typeof(IParentViewPropertyEditor))
                            .Select(item => listView.Editor.CallMethod("FindPropertyEditor",item,ViewEditMode.Edit) as IParentViewPropertyEditor)
                            .ToObservable().WhenNotDefault()
                            .Select(editor => (view: (ObjectView) listView, editor));
                    }

                    return Observable.Empty<(ObjectView view, IParentViewPropertyEditor editor)>();
                });

            var setupParentView = detailViewEditors.Merge(listViewEditors)
                .Do(_ => _.editor.SetParentView(_.view)).ToUnit();
            return setupParentView;
        }

    }

    public interface IParentViewPropertyEditor{
        void SetParentView(ObjectView value);
    }

}