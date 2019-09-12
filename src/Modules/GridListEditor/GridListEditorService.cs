using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.Utils.Extensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.GridListEditor{
    public static class GridListEditorService{
        internal static IObservable<TSource> TraceGridListEditorSelection<TSource>(this IObservable<TSource> source, string name = null,
            Action<string> traceAction = null,ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0){

            return source.Trace(name, GridListEditorModule.TraceSource, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        }


        internal static IObservable<Unit> Connect(this  XafApplication application){
            return application.RememberTopRow().ToUnit();
        }

        internal static IObservable<TSource> TraceGridListEditor<TSource>(this IObservable<TSource> source, string name = null, Action<string> traceAction = null,
            ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,[CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0){
            return source.Trace(name,GridListEditorModule.TraceSource,traceAction,traceStrategy,memberName,sourceFilePath,sourceLineNumber);
        }

        public static IObservable<string> RememberTopRow(this XafApplication application){
            var listViewFrame = application.WhenViewOnFrame(viewType:ViewType.ListView)
                .SelectMany(frame => ModelRules(application, frame).To(frame));
            return listViewFrame.Select(frame => frame.View).Cast<ListView>()
                .SelectMany(view => {
                    var gridListEditor = view.Editor.CastTo<DevExpress.ExpressApp.Win.Editors.GridListEditor>();
                    var topRowIndex = gridListEditor.GridView.TopRowIndex;
                    return view.CollectionSource.WhenCollectionReloaded()
                        .Do(_ => {
                            gridListEditor.GridView.TopRowIndex = topRowIndex;
                        })
                        .To($"TopRowIndex: {topRowIndex}, View: {view}");
                })
                .TraceGridListEditor();
        }

        private static IObservable<IModelGridListEditorTopRow> ModelRules(XafApplication application, Frame frame){
            return application.ReactiveModulesModel().GridListEditor().Rules().OfType<IModelGridListEditorTopRow>()
                .Where(row =>row.ListView == frame.View.Model &&((ListView) frame.View).Editor is DevExpress.ExpressApp.Win.Editors.GridListEditor)
                .TraceGridListEditor();
        }
    }
}