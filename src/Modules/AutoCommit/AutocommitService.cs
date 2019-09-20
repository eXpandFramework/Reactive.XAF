using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.Source.Extensions.System.AppDomain;
using Xpand.Source.Extensions.XAF.Model;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.AutoCommit{
    public static class AutoCommitService{
        private static readonly MethodInvoker AsssignClientHanderSafe;

        static AutoCommitService(){
            AsssignClientHanderSafe = AppDomain.CurrentDomain.AssemblyDevExpressExpressAppWeb()?.TypeClientSideEventsHelper()?.AsssignClientHanderSafe();
        }

        public static IObservable<ObjectView> WhenAutoCommitObjectViewCreated(this XafApplication application){
            return application
                .WhenObjectViewCreated()
                .Where(objectView => ((IModelObjectViewAutoCommit) objectView.Model).AutoCommit)
                .TraceAutoCommit();
        }

        internal static IObservable<TSource> TraceAutoCommit<TSource>(this IObservable<TSource> source, string name = null,
            Action<string> traceAction = null,
            ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0){
            return source.Trace(name, AutoCommitModule.TraceSource, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        }

        internal static IObservable<View> Connect(this XafApplication application){
            return application != null ? application.WhenAutoCommitObjectViewCreated().Publish().RefCount().AutoCommit() : Observable.Empty<View>();
        }

        public static IObservable<View> AutoCommit(this  IObservable<ObjectView> objectViewCreated){
            return objectViewCreated
                .QueryCanClose()
                .Merge(objectViewCreated.QueryCanChangeCurrentObject())
                .Select(_ => {
                    _.view.ObjectSpace.CommitChanges();
                    return _.view;
                })
                .Merge(objectViewCreated.OfType<ListView>().ControlsCreated().Select(_ => BatchEditCommit(_.view)))
                .TraceAutoCommit();
        }

        private static View BatchEditCommit(ListView listView){
            if (listView.AllowEdit&& $"{listView.Model.GetValue("InlineEditMode")}" == "Batch" && listView.Editor.Control.GetType().Name == "ASPxGridView") {
                AsssignClientHanderSafe(null,listView.Editor.Control,"Init", GetInitScript(), "grid.Init");
                AsssignClientHanderSafe(null, listView.Editor.Control,"BatchEditStartEditing", "function(s, e) { clearTimeout(s.timerHandle); }", "grid.BatchEditStartEditing");
                AsssignClientHanderSafe(null, listView.Editor.Control,"BatchEditEndEditing", "function(s, e) { s.timerHandle = setTimeout(function() { s.UpdateEdit();}, 100); }", "grid.BatchEditEndEditing");
                AsssignClientHanderSafe(null, listView.Editor.Control,"EndCallback", GetInitScript(), "grid.EndCallback");
            }
            return listView;
        }

        private static string GetInitScript() {
            return @"function(s, e) { 
                        s.timerHandle = -1; 
                        for (var i = 0; i < s.GetColumnsCount() ; i++) {
                            var editor = s.GetEditor(i);
				            if (!!editor)
				                ASPxClientUtils.AttachEventToElement(editor.GetMainElement(), ""onblur"", function(){s.batchEditApi.EndEdit();s.UpdateEdit();});
                        }
                    }";
        }

    }
}