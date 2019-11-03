using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Win.SystemModule;
using Fasterflect;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Win.Services;

namespace Xpand.XAF.Modules.OneView{
    public static class OneViewService{
        
        internal static IObservable<Unit> Connect(this  XafApplication application){

            var showView = application.ShowView().Publish().RefCount();
            return showView.EditModel(application)
                .Merge(showView.ExitApplication(application))
                .Merge(application.HideMainWindow())
                ;
        }

        private static IObservable<Unit> ExitApplication(this IObservable<ShowViewParameters> showView,XafApplication application){
            var editingModel = showView.SelectMany(_ =>_.Controllers.OfType<OneViewDialogController>().ToObservable()
                .SelectMany(controller => controller.AcceptAction.WhenExecuting().Select(tuple => tuple)));
            var closingView = Observable.Defer(() => showView.SelectMany(_ => _.CreatedView.WhenClosed())
                    .TakeUntil(editingModel))
                .Repeat()
                .Where(view => !(bool) application.GetFieldValue("exiting"))
                .Do(view => application.Exit())
                .Select(view => view);
            return closingView.ToUnit();

        }

        private static IObservable<Unit> EditModel(this IObservable<ShowViewParameters> showView,XafApplication application){
            var editModelAction = showView.SelectMany(_ => _.Controllers.OfType<OneViewDialogController>())
                .SelectMany(_ => _.AcceptAction.WhenExecuteCompleted()
                    .Select(tuple => application.MainWindow.GetController<EditModelController>().EditModelAction));
            return editModelAction
                .Do(action => action.DoExecute()).ToUnit()
                .TraceOneView();
        }

        private static IObservable<Unit> HideMainWindow(this XafApplication application){
            return application.WhenMainFormVisible()
                .Do(window => {
                    window.Template.ToForm().Visible = false;
                })
                .ToUnit()
                .TakeUntil(application.ToReactiveModule<IModelReactiveModuleOneView>().Where(view => view.OneView.View==null))
                .TraceOneView();
        }


        internal static IObservable<TSource> TraceOneView<TSource>(this IObservable<TSource> source, string name = null,
            Action<string> traceAction = null,ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0){

            return source.Trace(name, OneViewModule.TraceSource, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        }

        private static IObservable<ShowViewParameters> ShowView(this XafApplication application){
            return application.WhenWindowCreated().When(TemplateContext.ApplicationWindow)
                .SelectMany(window => {
                    var modelView = application.Model.ToReactiveModule<IModelReactiveModuleOneView>().OneView;
                    if (modelView.View!=null){
                        var showViewParameters = new ShowViewParameters();
                        var dialogController = new OneViewDialogController();
                        dialogController.AcceptAction.Caption = "Configure";
                        dialogController.CancelAction.Active[""] = false;
                        showViewParameters.Controllers.Add(dialogController);
                        showViewParameters.NewWindowTarget = NewWindowTarget.Separate;
                        showViewParameters.Context = TemplateContext.PopupWindow;

                        showViewParameters.TargetWindow = TargetWindow.NewWindow;
                        showViewParameters.CreatedView = application.CreateView(modelView.View);
                        application.ShowViewStrategy.ShowView(showViewParameters, new ShowViewSource(null, null));
                        return showViewParameters.AsObservable();
                    }

                    return Observable.Empty<ShowViewParameters>();
                })
                .TraceOneView()
                .WhenNotDefault();
        }
    }

    public class OneViewDialogController:DialogController{
        
    }
}