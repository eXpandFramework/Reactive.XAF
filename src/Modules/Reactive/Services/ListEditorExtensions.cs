using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Editors;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ListEditorExtensions{
        public static IObservable<(ListEditor editor, NewObjectAddingEventArgs e)> WhenNewObjectAdding(this ListEditor editor){
            return Observable.FromEventPattern<EventHandler<NewObjectAddingEventArgs>, NewObjectAddingEventArgs>(
                    handler => editor.NewObjectAdding += handler,
                    handler => editor.NewObjectAdding -= handler)
                .TransformPattern<NewObjectAddingEventArgs, ListEditor>();
        }

        public static IObservable<(ListEditor editor, EventArgs e)> WhenProcessSelectedItem(this ListEditor editor){
            return Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => editor.ProcessSelectedItem += handler,
                    handler => editor.ProcessSelectedItem -= handler)
                .TransformPattern<EventArgs, ListEditor>();
        }
    }
}