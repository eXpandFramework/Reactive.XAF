using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using NUnit.Framework;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;

namespace Xpand.XAF.Modules.Reactive.Tests.FaultContextTests{
    public delegate void ActionConfig(ActionBase action, XafApplication view);
    public delegate IObservable<ActionBase> ExecutionPipe(IObservable<ActionBase> source);
    public delegate ActionBase ActionFactory(Controller controller, string id);
    public delegate IObservable<ActionBase> ActionRepeat(ActionBase actionBase);
    public class FaultContextActionEventSelectors {
        public static IEnumerable<TestCaseData> ExecutionSelectors() {
            ActionFactory NewSimpleAction() => (frame, id) => new SimpleAction(frame, id, "Test");
            ActionFactory NewParametrizedAction() => (frame, id) => new ParametrizedAction(frame, id, "Test",typeof(int));
            ActionFactory NewSingleChoiceAction() => (frame, id) => new SingleChoiceAction(frame, id, "Test"){Items = { new ChoiceActionItem("Test Item",null) }};
            ActionFactory NewPopupWindowAction() => (frame, id) => {
                var popupWindowShowAction = new PopupWindowShowAction(frame, id, "Test") ;
                popupWindowShowAction.CustomizePopupWindowParams += (_, args) => {
                    args.View = frame.Application.NewListView(typeof(R));
                    args.Context = TemplateContext.View;
                };
                return popupWindowShowAction;
            };

            TestCaseData NewTestData<TEventArg,TAction>(ActionFactory actionFactory,string eventName,ActionConfig actionConfig,ActionRepeat actionRepeat) where TEventArg : EventArgs where  TAction : ActionBase 
                => new TestCaseData(
                    new ExecutionPipe(source => source.When<TEventArg,TAction>(eventName,_ => Observable.Throw<TAction>(new Exception("Test")))),
                    actionFactory,actionConfig,actionRepeat, $"When{eventName}"
                ).SetName($"{typeof(TAction).Name}_{eventName})");
            TestCaseData NewConcatExecutionData<TAction>(ActionFactory actionFactory,ActionConfig actionConfig,ActionRepeat actionRepeat) where  TAction : ActionBase 
                => new TestCaseData(
                    new ExecutionPipe(source => source.WhenConcatExecution(_ => 100.Milliseconds().Timer().SelectMany(_ => Observable.Throw<TAction>(new Exception())))),
                    actionFactory,actionConfig,actionRepeat, nameof(ActionsService.WhenConcatExecution)
                ).SetName($"{typeof(TAction).Name}_ConcatExecution)");


            var config = new ActionConfig((_, _) => {});    
            var repeatWhenObserved = new ActionRepeat(a => a.Observe());
            var repeatWhenEnabled = new ActionRepeat(a => a.WhenEnabled());
            yield return NewTestData<SimpleActionExecuteEventArgs,SimpleAction>(NewSimpleAction(),nameof(ActionBase.Executed),config,repeatWhenObserved);
            // yield return NewTestData<SimpleActionExecuteEventArgs,SimpleAction>(NewSimpleAction(),nameof(ActionBase.ExecuteCompleted),config,repeatWhenObserved);
            // yield return NewTestData<CancelEventArgs,SimpleAction>(NewSimpleAction(),nameof(ActionBase.Executing),config,repeatWhenObserved);
            // yield return NewTestData<SimpleActionExecuteEventArgs,SimpleAction>(NewSimpleAction(),nameof(SimpleAction.Execute),config,repeatWhenObserved);
            //
            // yield return NewConcatExecutionData<SimpleAction>(NewSimpleAction(),config,repeatWhenEnabled);
            //
            // yield return NewTestData<ParametrizedActionExecuteEventArgs,ParametrizedAction>(NewParametrizedAction(),nameof(ActionBase.Executed),config,repeatWhenObserved);
            // yield return NewTestData<ParametrizedActionExecuteEventArgs,ParametrizedAction>(NewParametrizedAction(),nameof(ActionBase.ExecuteCompleted),config,repeatWhenObserved);
            // yield return NewTestData<CancelEventArgs,ParametrizedAction>(NewParametrizedAction(),nameof(ActionBase.Executing),config,repeatWhenObserved);
            // yield return NewTestData<ParametrizedActionExecuteEventArgs,ParametrizedAction>(NewParametrizedAction(),nameof(SimpleAction.Execute),config,repeatWhenObserved);
            // yield return NewConcatExecutionData<ParametrizedAction>(NewParametrizedAction(),config,repeatWhenEnabled);
            //
            // yield return NewTestData<SingleChoiceActionExecuteEventArgs,SingleChoiceAction>(NewSingleChoiceAction(),nameof(ActionBase.Executed),config,repeatWhenObserved);
            // yield return NewTestData<SingleChoiceActionExecuteEventArgs,SingleChoiceAction>(NewSingleChoiceAction(),nameof(ActionBase.ExecuteCompleted),config,repeatWhenObserved);
            // yield return NewTestData<CancelEventArgs,SingleChoiceAction>(NewSingleChoiceAction(),nameof(ActionBase.Executing),config,repeatWhenObserved);
            // yield return NewTestData<SingleChoiceActionExecuteEventArgs,SingleChoiceAction>(NewSingleChoiceAction(),nameof(SingleChoiceAction.Execute),config,repeatWhenObserved);
            // yield return NewConcatExecutionData<SingleChoiceAction>(NewSingleChoiceAction(),config,repeatWhenEnabled);
            //
            // config = (action, application)
            //     => ((PopupWindowShowAction)action).CustomizePopupWindowParams += (_, args) => {
            //     args.View = application.NewListView(typeof(R));
            //     args.Context = TemplateContext.View;
            // };
            // yield return NewTestData<PopupWindowShowActionExecuteEventArgs,PopupWindowShowAction>(NewPopupWindowAction(),nameof(ActionBase.Executed),config,repeatWhenObserved);
            // yield return NewTestData<PopupWindowShowActionExecuteEventArgs,PopupWindowShowAction>(NewPopupWindowAction(),nameof(ActionBase.ExecuteCompleted),config,repeatWhenObserved);
            // yield return NewTestData<CancelEventArgs,PopupWindowShowAction>(NewPopupWindowAction(),nameof(ActionBase.Executing),config,repeatWhenObserved);
            // yield return NewTestData<PopupWindowShowActionExecuteEventArgs,PopupWindowShowAction>(NewPopupWindowAction(),nameof(PopupWindowShowAction.Execute),config,repeatWhenObserved);
            // yield return NewConcatExecutionData<PopupWindowShowAction>(NewPopupWindowAction(),config,repeatWhenEnabled);
        }
    }
}