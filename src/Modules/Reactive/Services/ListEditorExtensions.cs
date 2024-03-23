using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Editors;
using Fasterflect;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ListEditorExtensions {
        public static IObservable<TListEditor> WhenControlsCreated<TListEditor>(this TListEditor listEditor) where TListEditor:ListEditor 
            => listEditor.WhenEvent(nameof(listEditor.ControlsCreated)).StartWith(listEditor.Control).WhenNotDefault().To(listEditor);
        public static IObservable<T> UpdateGridView<T>(this ListEditor editor, Func<IEnumerable<T>> selector)
            => editor.UpdateGridView(() => selector().ToNowObservable());
        
        public static void BeginGridViewDataUpdate(this ListEditor editor)
            => editor.GridView()?.CallMethod("BeginDataUpdate");

        public static object GridView(this ListEditor editor) => editor.TryGetPropertyValue("GridView");

        public static void EndGridViewDataUpdate(this ListEditor editor)
            => editor.GridView()?.CallMethod("EndDataUpdate");
        
        public static IObservable<T> UpdateGridView<T>(this ListEditor editor,Func<IObservable<T>> selector,bool endOnNext=false) 
            => editor.Defer(() => {
                editor.BeginGridViewDataUpdate();
                return selector().DoWhen(_ => endOnNext,_ => editor.EndGridViewDataUpdate());

            }).Finally(() => {
                if (endOnNext)return;
                editor.EndGridViewDataUpdate();
            });
        
        public static IObservable<ListEditor> TakeUntilDisposed(this IObservable<ListEditor> source)
            => source.TakeWhileInclusive(editor => !editor.IsDisposed);
        
        public static IObservable<ListEditor> WhenModelApplied(this ListEditor editor) 
            => editor.WhenEvent(nameof(ListEditor.ModelApplied)).To(editor).TakeUntilDisposed();

        public static IObservable<NewObjectAddingEventArgs> WhenNewObjectAdding(this ListEditor editor) 
            => editor.WhenEvent<NewObjectAddingEventArgs>(nameof(editor.NewObjectAdding));
        public static IObservable<ListEditor> WhenDatasourceChanged(this ListEditor editor) 
            => editor.WhenEvent(nameof(editor.DataSourceChanged)).To(editor).TakeUntilDisposed();

        public static IObservable<ListEditor> WhenProcessSelectedItem(this ListEditor editor) 
            => editor.WhenEvent(nameof(ListEditor.ProcessSelectedItem)).To(editor).TakeUntilDisposed();
    }
}