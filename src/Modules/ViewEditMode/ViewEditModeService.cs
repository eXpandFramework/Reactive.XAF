using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.ViewEditMode{
    public static class ViewEditModeService{
        internal static IObservable<TSource> TraceViewEditModeModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, ViewEditModeModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);


        internal static IObservable<Unit> Connect(this XafApplication application) =>
            application != null? application.WhenViewEditModeChanged().ToUnit()
                .Merge(application.HandleWebEditAction(), Scheduler.Immediate): Observable.Empty<Unit>();

        private static IObservable<Unit> HandleWebEditAction(this XafApplication application){
            var webModificationsController = application.WhenWindowCreated()
                .ToController("DevExpress.ExpressApp.Web.SystemModule.WebModificationsController")
                .Activated()
                .When(ViewType.DetailView)
                .Where(_ => {
                    var model = ((IModelDetailViewViewEditMode) _.Frame.View.Model);
                    return model.ViewEditMode == DevExpress.ExpressApp.Editors.ViewEditMode.View && model.LockViewEditMode;
                })
                .Retry(application)
                .Publish().RefCount();

            var editAction = webModificationsController
                .Select(_ => _.Actions.First(action => action.Id == "SwitchToEditMode")).Cast<SimpleAction>()
                .Retry(application)
                .Publish().RefCount();
            editAction.SelectMany(action => action.Enabled.WhenResultValueChanged()).Subscribe(tuple => { });
            var unLockEdit = editAction.SelectMany(_ => _.WhenExecuting()).FirstAsync()
                .Select(_ => {
                    ((IModelDetailViewViewEditMode) _.action.Controller.Frame.View.Model).LockViewEditMode = false;
                    return Unit.Default;
                });
            var lockEdit = editAction.SelectMany(_ => _.WhenExecuteCompleted()).Select(_ => _.action).FirstAsync()
                .Select(_ => {
                    ((IModelDetailViewViewEditMode) _.Controller.Frame.View.Model).LockViewEditMode = true;
                    return Unit.Default;
                });
            return unLockEdit.Merge(lockEdit)
                .TraceViewEditModeModule().ToUnit().Retry(application);
        }

        public static IObservable<DetailView> WhenViewEditModeAssigned(this XafApplication application) =>
            application.WhenDetailViewCreated()
                .Select(_ => {
                    var detailView = _.e.View;
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
                .Select(_ => {
                    _.e.Cancel = ((IModelDetailViewViewEditMode) _.detailView.Model).LockViewEditMode;
                    return _.detailView;
                })
                .TraceViewEditModeModule(view => view.Id);
    }
}