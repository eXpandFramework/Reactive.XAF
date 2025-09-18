using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Actions;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.Reactive.Services.Actions {
    public static partial class ActionsService {
    
        
        private static IObservable<T> WhenConcatExecution<T, TArgs>(this ActionBase action, Func<TArgs, IObservable<T>> resilientSelector) where TArgs : ActionBaseEventArgs {
            return action.WhenExecuted(e => {
                e.Action.Enabled[nameof(WhenConcatExecution)] = false;
                return resilientSelector((TArgs)e)
                    .ObserveOnContext()
                    .Finally(() => {
                        e.Action.ExecutionFinished();
                        e.Action.Enabled[nameof(WhenConcatExecution)] = true;
                    });
            }).PushStackFrame();
        }

        public static IObservable<T> WhenConcatExecution<T>(this SimpleAction simpleAction,Func<SimpleActionExecuteEventArgs,IObservable<T>> resilientSelector)
            => simpleAction.WhenConcatExecution<T,SimpleActionExecuteEventArgs>(resilientSelector).PushStackFrame();

        public static IObservable<T> WhenConcatExecution<T>(this SingleChoiceAction simpleAction,Func<SingleChoiceActionExecuteEventArgs,IObservable<T>> resilientSelector)
            => simpleAction.WhenConcatExecution<T,SingleChoiceActionExecuteEventArgs>(resilientSelector).PushStackFrame();
        
        public static IObservable<T> WhenConcatExecution<T>(this ParametrizedAction simpleAction,Func<ParametrizedActionExecuteEventArgs,IObservable<T>> resilientSelector)
            => simpleAction.WhenConcatExecution<T,ParametrizedActionExecuteEventArgs>(resilientSelector).PushStackFrame();
        
        public static IObservable<T> WhenConcatExecution<T>(this PopupWindowShowAction popupWindowShowAction,Func<PopupWindowShowActionExecuteEventArgs,IObservable<T>> resilientSelector)
            => popupWindowShowAction.WhenConcatExecution<T,PopupWindowShowActionExecuteEventArgs>(resilientSelector).PushStackFrame();

        public static IObservable<T> WhenConcatExecution<TAction, T>(this IObservable<TAction> source, Func<ActionBaseEventArgs, IObservable<T>> resilientSelector) where TAction:ActionBase
            => source.SelectMany(action => action.WhenConcatExecution(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame().PushStackFrame();

        public static IObservable<T> WhenConcatExecution<T>(this IObservable<SimpleAction> source, Func<SimpleActionExecuteEventArgs, IObservable<T>> resilientSelector)
            => source.SelectMany(action => action.WhenConcatExecution(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame().PushStackFrame();
        
        public static IObservable<T> WhenConcatExecution<T>(this IObservable<SingleChoiceAction> source,Func<SingleChoiceActionExecuteEventArgs,IObservable<T>> resilientSelector) 
            => source.SelectMany(action => action.WhenConcatExecution(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame().PushStackFrame();
        
        public static IObservable<T> WhenConcatExecution<T>(this IObservable<ParametrizedAction> source,Func<ParametrizedActionExecuteEventArgs,IObservable<T>> resilientSelector) 
            => source.SelectMany(action => action.WhenConcatExecution(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame().PushStackFrame();
        
        public static IObservable<T> WhenConcatExecution<T>(this IObservable<PopupWindowShowAction> source,Func<PopupWindowShowActionExecuteEventArgs,IObservable<T>> resilientSelector) 
            => source.SelectMany(action => action.WhenConcatExecution(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame().PushStackFrame();

        public static IObservable<SimpleAction> WhenConcatExecution(this IObservable<SimpleAction> source, Action<SimpleActionExecuteEventArgs> resilientSelector)
            => source.WhenConcatExecution(e => e.DeferAction(() => resilientSelector(e)).To<SimpleAction>().Concat(e.Action.Observe().Cast<SimpleAction>())).PushStackFrame();
        
        public static IObservable<SingleChoiceAction> WhenConcatExecution(this IObservable<SingleChoiceAction> source, Action<SingleChoiceActionExecuteEventArgs> resilientSelector)
            => source.WhenConcatExecution(e => e.DeferAction(() => resilientSelector(e)).To<SingleChoiceAction>().Concat(e.Action.Observe().Cast<SingleChoiceAction>())).PushStackFrame();
        
        public static IObservable<ParametrizedAction> WhenConcatExecution(this IObservable<ParametrizedAction> source, Action<ParametrizedActionExecuteEventArgs> resilientSelector)
            => source.WhenConcatExecution(e => e.DeferAction(() => resilientSelector(e)).To<ParametrizedAction>().Concat(e.Action.Observe().Cast<ParametrizedAction>())).PushStackFrame();
    }
}