using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Win.SystemModule;
using Fasterflect;
using Xpand.Extensions.Reactive.FaultHub;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.OneView{
    public static class OneViewService{
        
        internal static IObservable<Unit> Connect(this  ApplicationModulesManager manager) 
	        => manager.WhenApplication(application => {
		        var cleanStartupNavigationItem = CleanStartupNavigationItem(application);
		        var showView = application.ShowView().Publish().RefCount();
		        return application.EditModel().ToUnit()
			        .Merge(showView.ExitApplication(application))
			        .Merge(application.HideMainWindow())
			        .Merge(cleanStartupNavigationItem);
	        });

        static IObservable<Unit> CleanStartupNavigationItem(this XafApplication application) 
	        => application.WhenModelChanged()
		        .Do(modelApplication => ((IModelApplicationNavigationItems) modelApplication).NavigationItems.StartupNavigationItem = null)
		        .ToUnit();

        private static IObservable<Unit> ExitApplication(this IObservable<ShowViewParameters> showView,XafApplication application){
            var editingModel = showView.SelectMany(parameters =>parameters.Controllers.OfType<OneViewDialogController>().ToObservable()
                .SelectMany(controller => controller.AcceptAction.WhenExecuting(_ => Observable.Empty<SimpleAction>())));
            return  Observable.Defer(() => showView.SelectMany(parameters => parameters.CreatedView.WhenClosing()
			            .SelectMany(_ => parameters.CreatedView.WhenClosed().To(application.MainWindow)))
		            .TakeUntil(editingModel)).Repeat()
                .Do(window => window?.Close())
                .ToUnit();
        }

        public static IObservable<Unit> EditModel(this XafApplication application) {
	        return application.WhenFrame()
		        .ToController<OneViewDialogController>().SelectMany(controller => controller.AcceptAction.WhenExecuted(
			        e => e.DeferAction(()
				        => application.MainWindow.GetController<EditModelController>().EditModelAction.DoExecute())));
        }

        private static IObservable<Unit> HideMainWindow(this XafApplication application) 
        
	        => application.WhenWin().SelectMany(api => api.WhenMainFormVisible())
		        .Do(window => window.Template.SetPropertyValue("Visible",false))
		        .TraceOneView(window => window.Context)
		        .ToUnit()
		        .TakeUntil(application.ToReactiveModule<IModelReactiveModuleOneView>().Where(view => view.OneView.View==null));
        
        internal static IObservable<TSource> TraceOneView<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,Func<string> allMessageFactory = null,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
	        => source.Trace(name, OneViewModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy,allMessageFactory, memberName,sourceFilePath,sourceLineNumber);

        private static IObservable<ShowViewParameters> ShowView(this XafApplication application) 
	        => application.WhenWindowCreated().When(TemplateContext.ApplicationWindow)
		        .SelectManyItemResilient(_ => application.ShowOneViewParameters().ShowOneView())
		        .TraceOneView(parameters => parameters.CreatedView.Id)
		        .WhenNotDefault();

        public static IObservable<ShowViewParameters> ShowOneViewParameters(this XafApplication application,IObjectSpace objectSpace=null) {
            var modelView = application.Model.ToReactiveModule<IModelReactiveModuleOneView>().OneView;
            if (modelView.View!=null) {
                objectSpace ??= application.CreateObjectSpace(modelView.View.AsObjectView.ModelClass.TypeInfo.Type);
                var showViewParameters = new ShowViewParameters();
                var dialogController = new OneViewDialogController();
                dialogController.AcceptAction.Caption = "Configure";
                dialogController.CancelAction.Active[""] = false;
                showViewParameters.Controllers.Add(dialogController);
                showViewParameters.NewWindowTarget = NewWindowTarget.Separate;
                showViewParameters.Context = TemplateContext.PopupWindow;
                showViewParameters.TargetWindow = TargetWindow.NewWindow;
                showViewParameters.CreatedView = application.NewView(modelView.View,objectSpace);
                return showViewParameters.Observe();
            }

            return Observable.Empty<ShowViewParameters>();
        }

        [SuppressMessage("Usage", "XAF0022:Avoid calling the ShowViewStrategyBase.ShowView() method")]
        public static IObservable<ShowViewParameters> ShowOneView(this IObservable<ShowViewParameters> source) 
            => source.Do(parameters => ((CompositeView) parameters.CreatedView).Application().ShowViewStrategy
                .ShowView(parameters, new ShowViewSource(null, null)))
                .TraceOneView();
    }

    public class OneViewDialogController:DialogController{
	    public OneViewDialogController() => AcceptAction.ActionMeaning=ActionMeaning.Unknown;
    }
}