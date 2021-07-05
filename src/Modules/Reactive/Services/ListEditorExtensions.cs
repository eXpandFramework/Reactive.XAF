using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Editors;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ListEditorExtensions{
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