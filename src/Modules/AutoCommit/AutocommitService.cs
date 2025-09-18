using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.AppDomainExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.AutoCommit{
    public static class AutoCommitService{
        private static readonly MethodInvoker AsssignClientHanderSafe;

        static AutoCommitService() => AsssignClientHanderSafe = AppDomain.CurrentDomain.XAF().AssemblyDevExpressExpressAppWeb()?.TypeClientSideEventsHelper()?.AsssignClientHanderSafe();

        public static IObservable<ObjectView> WhenAutoCommitObjectViewCreated(this XafApplication application) =>
            application
                .WhenObjectViewCreated()
                .Where(objectView => ((IModelObjectViewAutoCommit) objectView.Model).AutoCommit)
                .TraceAutoCommit(view => view.Id);

        internal static IObservable<TSource> TraceAutoCommit<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,Func<string> allMessageFactory = null,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, AutoCommitModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy,allMessageFactory, memberName,sourceFilePath,sourceLineNumber);


        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) =>
            manager.WhenApplication(application => application.WhenAutoCommitObjectViewCreated().AutoCommit().ToUnit());

        public static IObservable<View> AutoCommit(this  IObservable<ObjectView> objectViewCreated) =>
            objectViewCreated
                .QueryCanClose()
                .Merge(objectViewCreated.QueryCanChangeCurrentObject())
                .SelectItemResilient(t => {
                    t.view.ObjectSpace.CommitChanges();
                    return t.view;
                })
                .Merge(objectViewCreated.OfType<ListView>().WhenControlsCreated().SelectItemResilient(BatchEditCommit))
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