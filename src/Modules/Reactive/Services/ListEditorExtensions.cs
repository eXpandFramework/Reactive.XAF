﻿using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Editors;
using Fasterflect;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ListEditorExtensions {
        public static IObservable<T> UpdateGridView<T>(this ListEditor editor, Func<IEnumerable<T>> selector)
            => editor.UpdateGridView(() => selector().ToNowObservable());
        
        public static IObservable<T> UpdateGridView<T>(this ListEditor editor,Func<IObservable<T>> selector) 
            => editor.Defer(() => {
                editor.GetPropertyValue("GridView")?.CallMethod("BeginDataUpdate");
                return selector();

            }).Finally(() => editor.GetPropertyValue("GridView")?.CallMethod("EndDataUpdate"));

        public static IObservable<ListEditor> WhenModelApplied(this ListEditor editor) 
            => editor.WhenEvent(nameof(ListEditor.ModelApplied)).TakeUntil(_ => editor.IsDisposed).To(editor);

        public static IObservable<NewObjectAddingEventArgs> WhenNewObjectAdding(this ListEditor editor) 
            => editor.WhenEvent<NewObjectAddingEventArgs>(nameof(editor.NewObjectAdding));

        public static IObservable<ListEditor> WhenProcessSelectedItem(this ListEditor editor) 
            => editor.WhenEvent(nameof(ListEditor.ProcessSelectedItem)).TakeUntil(_ => editor.IsDisposed).To(editor);
    }
}