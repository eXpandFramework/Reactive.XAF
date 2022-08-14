using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
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
                editor.GetPropertyValue("GridView").CallMethod("BeginDataUpdate");
                return selector();

            }).Finally(() => editor.GetPropertyValue("GridView").CallMethod("EndDataUpdate"));

        public static IObservable<(ListEditor sender, EventArgs e)> WhenModelApplied(this ListEditor editor) 
            => Observable.FromEventPattern<EventHandler<EventArgs>, EventArgs>(
                    handler => editor.ModelApplied += handler,
                    handler => editor.ModelApplied -= handler,ImmediateScheduler.Instance)
                .TransformPattern<EventArgs, ListEditor>();

        public static IObservable<(ListEditor editor, NewObjectAddingEventArgs e)> WhenNewObjectAdding(this ListEditor editor) 
            => Observable.FromEventPattern<EventHandler<NewObjectAddingEventArgs>, NewObjectAddingEventArgs>(
                    handler => editor.NewObjectAdding += handler,
                    handler => editor.NewObjectAdding -= handler,ImmediateScheduler.Instance)
                .TransformPattern<NewObjectAddingEventArgs, ListEditor>();

        public static IObservable<(ListEditor editor, EventArgs e)> WhenProcessSelectedItem(this ListEditor editor) 
            => Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => editor.ProcessSelectedItem += handler,
                    handler => editor.ProcessSelectedItem -= handler,ImmediateScheduler.Instance)
                .TransformPattern<EventArgs, ListEditor>();
    }
}