using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.ViewWizard{
    public static class ViewWzardService{
        public static SingleChoiceAction ShowWizard(this (ViewWizardModule, Frame frame) tuple) => tuple
            .frame.Action(nameof(ShowWizard)).As<SingleChoiceAction>();

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager){
            var registerActions = manager.RegisterViewSingleChoiceAction(nameof(ShowWizard),
                action => action.Configure()).Publish().RefCount().ToUnit();
            return manager.WhenApplication(application => {
                var activeAction = application.ActiveAction();
                return registerActions.Merge(activeAction);
            });
            
        }

        internal static IObservable<TSource> TraceViewWizardModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, ViewWizardModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

        private static IObservable<Unit> ActiveAction(this XafApplication application) =>
            application.WhenViewOnFrame()
                .Where(frame => {
                    var modelDetailViews = application.Model.ToReactiveModule<IModelReactiveModulesViewWizard>().ViewWizard
                        .WizardViews.Select(view => view.DetailView);
                    return modelDetailViews.Contains(frame.View.Model);
                })
                .Do(frame => { frame.Action<ViewWizardModule>().ShowWizard().Active["Always"] = true; })
                .TraceViewWizardModule(frame => frame.View.Id)
                .ToUnit();

        private static void Configure(this SingleChoiceAction action){
            action.TargetViewType = ViewType.DetailView;
            action.Active["Always"] = false;
        }
    }
}
