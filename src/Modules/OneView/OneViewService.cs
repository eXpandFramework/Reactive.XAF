using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Win.SystemModule;
using Fasterflect;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Win.Services;

namespace Xpand.XAF.Modules.OneView{
    public static class OneViewService{
        
        internal static IObservable<Unit> Connect(this  XafApplication application){
            var showView = application.ShowView()
                .TakeUntil(application.WhenDisposed())
                .Publish();
            showView.Connect();
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
//                .Repeat()
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
                .TraceOneView();
        }


        internal static IObservable<TSource> TraceOneView<TSource>(this IObservable<TSource> source, string name = null,
            Action<string> traceAction = null,ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0){

            return source.Trace(name, OneViewModule.TraceSource, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        }


        

        private static IObservable<ShowViewParameters> ShowView(this XafApplication application){
            return application.ReactiveModulesModel().OneViewModel()
                .Do(view => {
                    ((IModelApplicationNavigationItems) view.Application).NavigationItems.StartupNavigationItem = null;
                    ((IModelOptionsWin) view.Application.Options).UIType=UIType.MultipleWindowSDI;
                })
                .SelectMany(modelView => {
                    return application.WhenMainFormShown().Select(window => {
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
                        
                        return showViewParameters;
                    });
                })
                .TraceOneView();
        }
    }

    public class OneViewDialogController:DialogController{
        
    }
}