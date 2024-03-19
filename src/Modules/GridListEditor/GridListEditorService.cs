using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.GridListEditor{
    internal static class GridListEditorService {
        internal static string ListEditorTypeName = "DevExpress.ExpressApp.Win.Editors.GridListEditor";
        internal static IObservable<Unit> Connect(this  ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.RememberTopRow().Merge(application.FocusRow()));

        private static IObservable<(Frame frame,TRule rule)> WhenRulesOnView<TRule>(this XafApplication application) where TRule:IModelGridListEditorRule
            => application.WhenViewOnFrame(viewType: ViewType.ListView)
                .SelectMany(frame => application.ModelRules<TRule>(frame).Select(rule => (frame,rule)));

        static IObservable<Unit> FocusRow(this XafApplication application)
            => application.WhenRulesOnView<IModelGridListEditorFocusRow>()
                .SelectMany(t => {
                    var gridView = t.frame.View.AsListView().GridView();
                    return t.rule.FocusRow(gridView).Merge(t.rule.MoveFocus( gridView));
                })
                .ToUnit();

        private static IObservable<Unit> FocusRow(this IModelGridListEditorFocusRow rule, object gridView) 
            => AppDomain.CurrentDomain.GridControlHandles().Where(info => info.Name == rule.RowHandle)
                .Select(info => info.GetValue(null))
                .Do(o => gridView.SetPropertyValue("FocusedRowHandle", o))
                .ToObservable().ToUnit();

        private static IObservable<Unit> MoveFocus(this IModelGridListEditorFocusRow rule, object gridView) 
            => gridView.WhenEvent("KeyDown").Where(p =>(int)gridView.GetPropertyValue("FocusedRowHandle")==0&& p.EventArgs.GetPropertyValue("KeyCode").ToString()=="Up")
                .SelectMany(_ => AppDomain.CurrentDomain.GridControlHandles().Where(info => info.Name == rule.UpArrowMoveToRowHandle)
                    .Select(info => info.GetValue(null))
                    .Do(o => gridView.SetPropertyValue("FocusedRowHandle", o))).ToUnit();

        static IObservable<Unit> RememberTopRow(this XafApplication application) 
            => application.WhenRulesOnView<IModelGridListEditorTopRow>().Select(t => t.frame).MergeIgnored(frame => {
                var view = frame.View.AsListView();
                var gridView = view.GridView();
                var topRowIndex = gridView.GetPropertyValue("TopRowIndex");
                return view.CollectionSource.WhenCollectionReloaded()
                    .Do(_ => gridView.SetPropertyValue("TopRowIndex", topRowIndex))
                    .To($"TopRowIndex: {topRowIndex}, View: {view}")
                    .TraceGridListEditor();
            }).ToUnit();

        private static object GridView(this ListView view) => view.Editor.GetPropertyValue("GridView");

        private static IObservable<TRule> ModelRules<TRule>(this XafApplication application, Frame frame) where TRule:IModelGridListEditorRule
            => application.ReactiveModulesModel().GridListEditor().Rules().OfType<TRule>()
		        .Where(row =>row.ListView == frame.View.Model &&((ListView) frame.View).Editor.GetType().InheritsFrom(ListEditorTypeName) )
		        .TraceGridListEditor(row => row.ListView.Id);

        internal static IObservable<TSource> TraceGridListEditor<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,Func<string> allMessageFactory = null,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, GridListEditorModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy,allMessageFactory, memberName,sourceFilePath,sourceLineNumber);
    }
}