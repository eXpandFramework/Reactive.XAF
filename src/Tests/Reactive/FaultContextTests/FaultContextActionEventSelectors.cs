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
    public delegate IObservable<ActionBase> ResilienceOperator(IObservable<ActionBase> source);
    public class FaultContextActionEventSelectors {
        
        public static IEnumerable<TestCaseData> ExecutionSelectors() {
            ActionFactory newSimpleAction = (c, id) => new SimpleAction(c, id, "Test");
            ActionFactory newParametrizedAction = (c, id) => new ParametrizedAction(c, id, "Test", typeof(int));
            ActionRepeat repeatWhenObserved = a => a.Observe();
            ActionConfig config = (_, _) => { };
            ActionRepeat repeatWhenEnabled = a => a.WhenEnabled();
            ActionFactory newSingleChoiceAction = (c, id) => new SingleChoiceAction(c, id, "Test") { Items = { new ChoiceActionItem("Test Item", null) } };
            ActionFactory newPopupWindowAction = (frame, id) => {
                var popupWindowShowAction = new PopupWindowShowAction(frame, id, "Test");
                popupWindowShowAction.CustomizePopupWindowParams += (_, args) => {
                    args.View = frame.Application.NewListView(typeof(R));
                    args.Context = TemplateContext.View;
                };
                return popupWindowShowAction;
            };
            
            ResilienceOperator WhenEventOperator<TEvent>(string eventName) where TEvent : EventArgs
                => source => source.When<TEvent, ActionBase>(eventName, _ => Observable.Throw<ActionBase>(new Exception("Test")));

            ResilienceOperator WhenConcatOperator()
                => source => source.WhenConcatExecution(_ => 100.Milliseconds().Timer().SelectMany(_ => Observable.Throw<ActionBase>(new Exception("Test"))));
            
            yield return new TestCaseData(WhenEventOperator<SimpleActionExecuteEventArgs>(nameof(SimpleAction.Executed)),
                newSimpleAction, config, repeatWhenObserved).SetName("SimpleAction_Executed_Resilience");

            yield return new TestCaseData(WhenEventOperator<SimpleActionExecuteEventArgs>(nameof(SimpleAction.ExecuteCompleted)),
                newSimpleAction, config, repeatWhenObserved).SetName("SimpleAction_ExecuteCompleted_Resilience");

            yield return new TestCaseData(WhenEventOperator<CancelEventArgs>(nameof(SimpleAction.Executing)),
                newSimpleAction, config, repeatWhenObserved).SetName("SimpleAction_Executing_Resilience");

            yield return new TestCaseData(WhenEventOperator<SimpleActionExecuteEventArgs>(nameof(SimpleAction.Execute)),
                newSimpleAction, config, repeatWhenObserved).SetName("SimpleAction_Execute_Resilience");

            yield return new TestCaseData(WhenConcatOperator(),
                newSimpleAction, config, repeatWhenEnabled).SetName("SimpleAction_ConcatExecution_Resilience");

            yield return new TestCaseData(WhenEventOperator<SingleChoiceActionExecuteEventArgs>(nameof(SingleChoiceAction.ExecuteCompleted)),
                newSingleChoiceAction, config, repeatWhenObserved).SetName("SingleChoiceAction_ExecuteCompleted_Resilience");

            yield return new TestCaseData(WhenEventOperator<CancelEventArgs>(nameof(SingleChoiceAction.Executing)),
                newSingleChoiceAction, config, repeatWhenObserved).SetName("SingleChoiceAction_Executing_Resilience");

            yield return new TestCaseData(WhenEventOperator<SingleChoiceActionExecuteEventArgs>(nameof(SingleChoiceAction.Execute)),
                newSingleChoiceAction, config, repeatWhenObserved).SetName("SingleChoiceAction_Execute_Resilience");

            yield return new TestCaseData(WhenConcatOperator(),
                newSingleChoiceAction, config, repeatWhenEnabled).SetName("SingleChoiceAction_ConcatExecution_Resilience");
            
            yield return new TestCaseData(WhenEventOperator<ParametrizedActionExecuteEventArgs>(nameof(ParametrizedAction.Executed)),
                newParametrizedAction, config, repeatWhenObserved).SetName("ParametrizedAction_Executed_Resilience");

            yield return new TestCaseData(WhenEventOperator<ParametrizedActionExecuteEventArgs>(nameof(ParametrizedAction.ExecuteCompleted)),
                newParametrizedAction, config, repeatWhenObserved).SetName("ParametrizedAction_ExecuteCompleted_Resilience");

            yield return new TestCaseData(WhenEventOperator<CancelEventArgs>(nameof(ParametrizedAction.Executing)),
                newParametrizedAction, config, repeatWhenObserved).SetName("ParametrizedAction_Executing_Resilience");

            yield return new TestCaseData(WhenEventOperator<ParametrizedActionExecuteEventArgs>(nameof(ParametrizedAction.Execute)),
                newParametrizedAction, config, repeatWhenObserved).SetName("ParametrizedAction_Execute_Resilience");

            yield return new TestCaseData(WhenConcatOperator(),
                newParametrizedAction, config, repeatWhenEnabled).SetName("ParametrizedAction_ConcatExecution_Resilience");
            
            yield return new TestCaseData(WhenEventOperator<PopupWindowShowActionExecuteEventArgs>(nameof(PopupWindowShowAction.Executed)),
                newPopupWindowAction, config, repeatWhenObserved).SetName("PopupWindowShowAction_Executed_Resilience");
            yield return new TestCaseData(WhenEventOperator<PopupWindowShowActionExecuteEventArgs>(nameof(PopupWindowShowAction.ExecuteCompleted)),
                newPopupWindowAction, config, repeatWhenObserved).SetName("PopupWindowShowAction_ExecuteCompleted_Resilience");
            yield return new TestCaseData(WhenEventOperator<CancelEventArgs>(nameof(PopupWindowShowAction.Executing)),
                newPopupWindowAction, config, repeatWhenObserved).SetName("PopupWindowShowAction_Executing_Resilience");
            yield return new TestCaseData(WhenEventOperator<PopupWindowShowActionExecuteEventArgs>(nameof(PopupWindowShowAction.Execute)),
                newPopupWindowAction, config, repeatWhenObserved).SetName("PopupWindowShowAction_Execute_Resilience");
            yield return new TestCaseData(WhenConcatOperator(),
                newPopupWindowAction, config, repeatWhenEnabled).SetName("PopupWindowShowAction_ConcatExecution_Resilience");
        }

    }
}