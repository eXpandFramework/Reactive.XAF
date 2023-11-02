using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.ViewEditMode{
    public static class ViewEditModeService{
        internal static IObservable<TSource> TraceViewEditModeModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, ViewEditModeModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);


        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) =>
            manager.WhenApplication(application => application.WhenViewEditModeChanged().ToUnit()
                .Merge(application.HandleWebEditAction(), Scheduler.Immediate));
            

        private static IObservable<Unit> HandleWebEditAction(this XafApplication application){
            var webModificationsController = application.WhenWindowCreated()
                .ToController("DevExpress.ExpressApp.Web.SystemModule.WebModificationsController")
                .Activated()
                .When(ViewType.DetailView)
                .Where(controller => {
                    var model = ((IModelDetailViewViewEditMode) controller.Frame.View.Model);
                    return model.ViewEditMode == DevExpress.ExpressApp.Editors.ViewEditMode.View && model.LockViewEditMode;
                })
                .Publish().RefCount();

            var editAction = webModificationsController
                .Select(controller => controller.Actions.First(action => action.Id == "SwitchToEditMode")).Cast<SimpleAction>()
                .Publish().RefCount();
            editAction.SelectMany(action => action.Enabled.WhenResultValueChanged()).Subscribe();
            var unLockEdit = editAction.SelectMany(action => action.WhenExecuting()).TakeFirst()
                .Select(t => {
                    ((IModelDetailViewViewEditMode) t.action.Controller.Frame.View.Model).LockViewEditMode = false;
                    return Unit.Default;
                });
            var lockEdit = editAction.SelectMany(action => action.WhenExecuteCompleted()).Select(e => e.Action).TakeFirst()
                .Select(actionBase => {
                    ((IModelDetailViewViewEditMode) actionBase.Controller.Frame.View.Model).LockViewEditMode = true;
                    return Unit.Default;
                });
            return unLockEdit.Merge(lockEdit)
                .TraceViewEditModeModule().ToUnit();
        }

        public static IObservable<DetailView> WhenViewEditModeAssigned(this XafApplication application) =>
            application.WhenDetailViewCreated()
                .Select(t => {
                    var detailView = t.e.View;
                    var viewEditMode = ((IModelDetailViewViewEditMode) detailView.Model).ViewEditMode;
                    if (viewEditMode != null){
                        detailView.ViewEditMode = viewEditMode.Value;
                        return detailView;
                    }

                    return null;
                }).WhenNotDefault()
                .TraceViewEditModeModule(view => view.Id);

        public static IObservable<DetailView> WhenViewEditModeChanged(this XafApplication application) =>
            application.WhenViewEditModeAssigned()
                .ViewEditModeChanging()
                .Select(t => {
                    t.e.Cancel = ((IModelDetailViewViewEditMode) t.detailView.Model).LockViewEditMode;
                    return t.detailView;
                })
                .TraceViewEditModeModule(view => view.Id);
    }
}