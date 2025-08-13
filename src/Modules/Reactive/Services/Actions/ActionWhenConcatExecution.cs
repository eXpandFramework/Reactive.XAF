using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp.Actions;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.Reactive.Services.Actions {
    public static partial class ActionsService {
    
        
        private static IObservable<T> WhenConcatExecution<T, TArgs>(this ActionBase action, Func<TArgs, IObservable<T>> resilientSelector, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) where TArgs : ActionBaseEventArgs {
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

        public static IObservable<T> WhenConcatExecution<T>(this SimpleAction simpleAction,Func<SimpleActionExecuteEventArgs,IObservable<T>> resilientSelector,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => simpleAction.WhenConcatExecution<T,SimpleActionExecuteEventArgs>(resilientSelector,memberName,filePath,lineNumber).PushStackFrame();

        public static IObservable<T> WhenConcatExecution<T>(this SingleChoiceAction simpleAction,Func<SingleChoiceActionExecuteEventArgs,IObservable<T>> resilientSelector,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => simpleAction.WhenConcatExecution<T,SingleChoiceActionExecuteEventArgs>(resilientSelector,memberName,filePath,lineNumber).PushStackFrame();
        
        public static IObservable<T> WhenConcatExecution<T>(this ParametrizedAction simpleAction,Func<ParametrizedActionExecuteEventArgs,IObservable<T>> resilientSelector,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => simpleAction.WhenConcatExecution<T,ParametrizedActionExecuteEventArgs>(resilientSelector,memberName,filePath,lineNumber).PushStackFrame();
        
        public static IObservable<T> WhenConcatExecution<T>(this PopupWindowShowAction popupWindowShowAction,Func<PopupWindowShowActionExecuteEventArgs,IObservable<T>> resilientSelector,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => popupWindowShowAction.WhenConcatExecution<T,PopupWindowShowActionExecuteEventArgs>(resilientSelector,memberName,filePath,lineNumber).PushStackFrame();

        public static IObservable<T> WhenConcatExecution<TAction, T>(this IObservable<TAction> source, Func<ActionBaseEventArgs, IObservable<T>> resilientSelector, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) where TAction:ActionBase
            => source.SelectMany(action => action.WhenConcatExecution(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame(memberName,filePath,lineNumber).PushStackFrame();

        public static IObservable<T> WhenConcatExecution<T>(this IObservable<SimpleAction> source, Func<SimpleActionExecuteEventArgs, IObservable<T>> resilientSelector,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.SelectMany(action => action.WhenConcatExecution(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame(memberName,filePath,lineNumber).PushStackFrame();
        
        public static IObservable<T> WhenConcatExecution<T>(this IObservable<SingleChoiceAction> source,Func<SingleChoiceActionExecuteEventArgs,IObservable<T>> resilientSelector,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) 
            => source.SelectMany(action => action.WhenConcatExecution(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame(memberName,filePath,lineNumber).PushStackFrame();
        
        public static IObservable<T> WhenConcatExecution<T>(this IObservable<ParametrizedAction> source,Func<ParametrizedActionExecuteEventArgs,IObservable<T>> resilientSelector,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) 
            => source.SelectMany(action => action.WhenConcatExecution(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame(memberName,filePath,lineNumber).PushStackFrame();
        
        public static IObservable<T> WhenConcatExecution<T>(this IObservable<PopupWindowShowAction> source,Func<PopupWindowShowActionExecuteEventArgs,IObservable<T>> resilientSelector,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) 
            => source.SelectMany(action => action.WhenConcatExecution(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame(memberName,filePath,lineNumber).PushStackFrame();

        public static IObservable<SimpleAction> WhenConcatExecution(this IObservable<SimpleAction> source, Action<SimpleActionExecuteEventArgs> resilientSelector,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.WhenConcatExecution(e => e.DeferAction(() => resilientSelector(e)).To<SimpleAction>().Concat(e.Action.Observe().Cast<SimpleAction>()),memberName,filePath,lineNumber).PushStackFrame();
        
        public static IObservable<SingleChoiceAction> WhenConcatExecution(this IObservable<SingleChoiceAction> source, Action<SingleChoiceActionExecuteEventArgs> resilientSelector,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.WhenConcatExecution(e => e.DeferAction(() => resilientSelector(e)).To<SingleChoiceAction>().Concat(e.Action.Observe().Cast<SingleChoiceAction>()),memberName,filePath,lineNumber).PushStackFrame();
        
        public static IObservable<ParametrizedAction> WhenConcatExecution(this IObservable<ParametrizedAction> source, Action<ParametrizedActionExecuteEventArgs> resilientSelector,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.WhenConcatExecution(e => e.DeferAction(() => resilientSelector(e)).To<ParametrizedAction>().Concat(e.Action.Observe().Cast<ParametrizedAction>()),memberName,filePath,lineNumber).PushStackFrame();
    }
}