using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.GridListEditor{
    public static class GridListEditorService{

        internal static IObservable<Unit> Connect(this  XafApplication application){
            return application.RememberTopRow().Retry(application).ToUnit();
        }
        internal static IObservable<TSource> TraceGridListEditor<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
	        source.Trace(name, GridListEditorModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);


        public static IObservable<string> RememberTopRow(this XafApplication application) =>
	        application.WhenViewOnFrame(viewType:ViewType.ListView)
		        .SelectMany(frame => ModelRules(application, frame).To(frame))
		        .Select(frame => frame.View).Cast<ListView>()
		        .SelectMany(view => {
			        var gridListEditor = ((DevExpress.ExpressApp.Win.Editors.GridListEditor) view.Editor);
			        var topRowIndex = gridListEditor.GridView.TopRowIndex;
			        return view.CollectionSource.WhenCollectionReloaded()
				        .Do(_ => {
					        gridListEditor.GridView.TopRowIndex = topRowIndex;
				        })
				        .To($"TopRowIndex: {topRowIndex}, View: {view}");
		        })
		        .TraceGridListEditor();

        private static IObservable<IModelGridListEditorTopRow> ModelRules(XafApplication application, Frame frame) =>
	        application.ReactiveModulesModel().GridListEditor().Rules().OfType<IModelGridListEditorTopRow>()
		        .Where(row =>row.ListView == frame.View.Model &&((ListView) frame.View).Editor is DevExpress.ExpressApp.Win.Editors.GridListEditor)
		        .TraceGridListEditor(row => row.ListView.Id);
    }
}