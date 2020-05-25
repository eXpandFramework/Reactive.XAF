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
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Win.Services;

namespace Xpand.XAF.Modules.OneView{
    public static class OneViewService{
        
        internal static IObservable<Unit> Connect(this  XafApplication application){
            var cleanStartupNavigationItem = CleanStartupNavigationItem(application);
            var showView = application.ShowView().Publish().RefCount();
            return showView.EditModel(application)
                .Merge(showView.ExitApplication(application))
                .Merge(application.HideMainWindow())
                .Merge(cleanStartupNavigationItem)
                ;
        }

        static IObservable<Unit> CleanStartupNavigationItem(this XafApplication application) => application.WhenModelChanged()
		        .Do(modelApplication => ((IModelApplicationNavigationItems) modelApplication).NavigationItems.StartupNavigationItem = null)
		        .ToUnit();

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

        private static IObservable<Unit> EditModel(this IObservable<ShowViewParameters> showView,XafApplication application) =>
	        showView.SelectMany(_ => _.Controllers.OfType<OneViewDialogController>())
		        .SelectMany(_ => _.AcceptAction.WhenExecuteCompleted()
			        .Select(tuple => application.MainWindow.GetController<EditModelController>().EditModelAction))
		        .Do(action => action.DoExecute()).ToUnit()
		        .TraceOneView();

        private static IObservable<Unit> HideMainWindow(this XafApplication application) =>
	        application.WhenMainFormVisible()
		        .Do(window => {
			        window.Template.ToForm().Visible = false;
		        })
		        .TraceOneView(window => window.Context)
		        .ToUnit()
		        .TakeUntil(application.ToReactiveModule<IModelReactiveModuleOneView>().Where(view => view.OneView.View==null));
		        

        internal static IObservable<TSource> TraceOneView<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
	        source.Trace(name, OneViewModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

        private static IObservable<ShowViewParameters> ShowView(this XafApplication application) =>
	        application.WhenWindowCreated().When(TemplateContext.ApplicationWindow)
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
				        showViewParameters.CreatedView = application.NewView(modelView.View);
				        application.ShowViewStrategy.ShowView(showViewParameters, new ShowViewSource(null, null));
				        return showViewParameters.ReturnObservable();
			        }

			        return Observable.Empty<ShowViewParameters>();
		        })
		        .TraceOneView(parameters => parameters.CreatedView.Id)
		        .WhenNotDefault();
    }

    public class OneViewDialogController:DialogController{
        
    }
}