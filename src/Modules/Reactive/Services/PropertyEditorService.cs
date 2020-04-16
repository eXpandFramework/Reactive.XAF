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
            var detailViewEditors = application.WhenDetailViewCreated()
                .SelectMany(_ => _.e.View.GetItems<IParentViewPropertyEditor>().Select(editor => (view: (ObjectView) _.e.View, editor)));
            var listViewEditors = application.WhenListViewCreated()
                .SelectMany(_ => _.e.ListView.WhenControlsCreated())
                .SelectMany(_ => {
                    if (_.view.Editor.GetType().InheritsFrom("DevExpress.ExpressApp.Web.Editors.ComplexWebListEditor")){
                        return _.view.Model.MemberViewItems(typeof(IParentViewPropertyEditor))
                            .Select(item => _.view.Editor.CallMethod("FindPropertyEditor",item,ViewEditMode.Edit) as IParentViewPropertyEditor)
                            .ToObservable().WhenNotDefault()
                            .Select(editor => (view: (ObjectView) _.view, editor));
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