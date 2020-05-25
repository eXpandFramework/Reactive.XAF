using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.Extensions.AppDomain;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.Model;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.AutoCommit{
    public static class AutoCommitService{
        private static readonly MethodInvoker AsssignClientHanderSafe;

        static AutoCommitService() => AsssignClientHanderSafe = AppDomain.CurrentDomain.AssemblyDevExpressExpressAppWeb()?.TypeClientSideEventsHelper()?.AsssignClientHanderSafe();

        public static IObservable<ObjectView> WhenAutoCommitObjectViewCreated(this XafApplication application) =>
            application
                .WhenObjectViewCreated()
                .Where(objectView => ((IModelObjectViewAutoCommit) objectView.Model).AutoCommit)
                .TraceAutoCommit(view => view.Id);

        internal static IObservable<TSource> TraceAutoCommit<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, AutoCommitModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);


        internal static IObservable<View> Connect(this XafApplication application) => application != null ? application.WhenAutoCommitObjectViewCreated().AutoCommit().Retry(application) : Observable.Empty<View>();

        public static IObservable<View> AutoCommit(this  IObservable<ObjectView> objectViewCreated) =>
            objectViewCreated
                .QueryCanClose()
                .Merge(objectViewCreated.QueryCanChangeCurrentObject())
                .Select(_ => {
                    _.view.ObjectSpace.CommitChanges();
                    return _.view;
                })
                .Merge(objectViewCreated.OfType<ListView>().ControlsCreated().Select(BatchEditCommit))
                .TraceAutoCommit(view => view.Id);

        private static View BatchEditCommit(ListView listView){
            if (listView.AllowEdit&& $"{listView.Model.GetValue("InlineEditMode")}" == "Batch" && listView.Editor.Control.GetType().Name == "ASPxGridView") {
                AsssignClientHanderSafe(null,listView.Editor.Control,"Init", GetInitScript(), "grid.Init");
                AsssignClientHanderSafe(null, listView.Editor.Control,"BatchEditStartEditing", "function(s, e) { clearTimeout(s.timerHandle); }", "grid.BatchEditStartEditing");
                AsssignClientHanderSafe(null, listView.Editor.Control,"BatchEditEndEditing", "function(s, e) { s.timerHandle = setTimeout(function() { s.UpdateEdit();}, 100); }", "grid.BatchEditEndEditing");
                AsssignClientHanderSafe(null, listView.Editor.Control,"EndCallback", GetInitScript(), "grid.EndCallback");
            }
            return listView;
        }

        private static string GetInitScript() =>
            @"function(s, e) { 
                        s.timerHandle = -1; 
                        for (var i = 0; i < s.GetColumnsCount() ; i++) {
                            var editor = s.GetEditor(i);
				            if (!!editor)
				                ASPxClientUtils.AttachEventToElement(editor.GetMainElement(), ""onblur"", function(){s.batchEditApi.EndEdit();s.UpdateEdit();});
                        }
                    }";
    }
}