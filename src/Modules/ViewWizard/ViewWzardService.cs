using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.Data.Extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.ViewWizard{
    public static class ViewWzardService{
        public static SingleChoiceAction ShowWizard(this (ViewWizardModule, Frame frame) tuple) => tuple
            .frame.Action(nameof(ShowWizard)).As<SingleChoiceAction>();
        public static SimpleAction NextWizardView(this (ViewWizardModule, Frame frame) tuple) => tuple
            .frame.Action(nameof(NextWizardView)).As<SimpleAction>();
        public static SimpleAction PreviousWizardView(this (ViewWizardModule, Frame frame) tuple) => tuple
            .frame.Action(nameof(PreviousWizardView)).As<SimpleAction>();
        public static SimpleAction FinishWizardView(this (ViewWizardModule, Frame frame) tuple) => tuple
            .frame.Action(nameof(FinishWizardView)).As<SimpleAction>();

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager){
            var registerActions = manager.RegisterViewSingleChoiceAction(nameof(ShowWizard), action => action.Configure()).Cast<ActionBase>()
                .Merge(manager.RegisterViewSimpleAction(nameof(NextWizardView), action => action.Configure(),PredefinedCategory.PopupActions))
                .Merge(manager.RegisterViewSimpleAction(nameof(PreviousWizardView), action => action.Configure(),PredefinedCategory.PopupActions))
                .Publish().RefCount();
            return manager.WhenApplication(application => {
                    var showWizardView = registerActions.OfType<SingleChoiceAction>().ShowWizardView().Publish().RefCount();
                    return showWizardView.NextWizardView().Merge(showWizardView.PreviousWizardView())
                        .ToUnit()
                        .Merge(application.ActiveAction().PopulateShowWizardActionItems());
                })
            .Merge(registerActions.ToUnit());
        }

        private static IObservable<SimpleActionExecuteEventArgs> NextWizardView(this IObservable<(Frame Frame, IModelWizardView modelWizardView)> source) 
            => source.SelectMany(tuple => {
                var nextWizardView = tuple.Frame.Action<ViewWizardModule>().NextWizardView();
                nextWizardView.Active["Always"] = true;
                return nextWizardView.WhenExecute()
                    .Do(args => {
                        tuple.Frame.Action<ViewWizardModule>().PreviousWizardView().Enabled["FirstView"] = true;
                        var wizardView = tuple.modelWizardView;
                        args.ShowViewParameters.CreatedView = args.Action.Application.NewView(tuple.Frame.NextChildDetailView(wizardView));
                    });
            });

        private static IObservable<SimpleActionExecuteEventArgs> PreviousWizardView(this IObservable<(Frame Frame, IModelWizardView modelWizardView)> source){
            return source.SelectMany(tuple => {
                var previousWizardView = tuple.Frame.Action<ViewWizardModule>().PreviousWizardView();
                previousWizardView.Active["Always"] = true;
                previousWizardView.Enabled["FirstView"] = false;
                return previousWizardView.WhenExecute()
                    .Do(args => {
                        var wizardView = tuple.modelWizardView;
                        args.ShowViewParameters.CreatedView = args.Action.Application.NewView(tuple.Frame.PreviousChildDetailView(wizardView));
                    });
            });
        }

        private static IModelDetailView NextChildDetailView(this Frame frame, IModelWizardView wizardView){
            if (frame.View.Model == wizardView.DetailView){
                return wizardView.Childs.First().ChildDetailView;
            }
            var currentWizardIndex = wizardView.GetParent<IModelWizardViews>().FindIndex(view => view.DetailView==frame.View.Model);
            return  ((IModelWizardViews) wizardView.Parent)[currentWizardIndex + 1].DetailView;
        }
        private static IModelDetailView PreviousChildDetailView(this Frame frame, IModelWizardView wizardView){
            if (frame.View.Model == wizardView.DetailView){
                return wizardView.Childs.First().ChildDetailView;
            }
            var currentWizardIndex = wizardView.GetParent<IModelWizardViews>().FindIndex(view => view.DetailView==frame.View.Model);
            return  ((IModelWizardViews) wizardView.Parent)[currentWizardIndex + 1].DetailView;
        }

        private static IObservable<(Frame Frame, IModelWizardView modelWizardView)> ShowWizardView(this IObservable<SingleChoiceAction> registerActions) =>
            registerActions.WhenExecute()
                .SelectMany(e => {
                    var modelWizardView = ((IModelWizardView) e.SelectedChoiceActionItem.Data);
                    var parameters = e.ShowViewParameters;
                    parameters.CreatedView = e.Action.Application.NewView(modelWizardView.DetailView);
                    parameters.CreateAllControllers = true;
                    var dialogController = new DialogController();
                    
                    parameters.Controllers.Add(dialogController);
                    parameters.TargetWindow=TargetWindow.NewModalWindow;
                    return dialogController.WhenActivated().Select(controller => (controller.Frame,modelWizardView));
                });

        internal static IObservable<TSource> TraceViewWizardModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, ViewWizardModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

        private static IObservable<Frame> ActiveAction(this XafApplication application) =>
            application.WhenViewOnFrame()
                .Where(frame => application.Model.ToReactiveModule<IModelReactiveModulesViewWizard>().ViewWizard
                    .WizardViews.Select(view => view.DetailView).Contains(frame.View.Model))
                .Do(frame => { frame.Action<ViewWizardModule>().ShowWizard().Active["Always"] = true; })
                .TraceViewWizardModule(frame => frame.View.Id);

        private static IObservable<Unit> PopulateShowWizardActionItems(this IObservable<Frame> source) 
            => source.SelectMany(frame => {
                    var singleChoiceAction = frame.Action<ViewWizardModule>().ShowWizard();
                    return frame.Application.Model.ToReactiveModule<IModelReactiveModulesViewWizard>().ViewWizard.WizardViews
                        .Do(modelWizardView => {
                            singleChoiceAction.Items.Add(new ChoiceActionItem(modelWizardView.DetailView.Caption, modelWizardView));
                        });
                })
                .ToUnit();

        

        private static void Configure(this ActionBase action){
            action.TargetViewType = ViewType.DetailView;
            action.Active["Always"] = false;
        }


    }
}
