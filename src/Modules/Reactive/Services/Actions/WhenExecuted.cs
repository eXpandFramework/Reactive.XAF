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
        public static IObservable<T> WhenExecuted<T>(this ActionBase action,Func<ActionBaseEventArgs,IObservable<T>> resilientSelector,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) 
            => action.ProcessEvent(nameof(ActionBase.Executed),resilientSelector).TakeUntilDisposed(action).PushStackFrame(memberName,filePath,lineNumber);
        
        public static IObservable<ActionBaseEventArgs> WhenExecuted(this ActionBase action,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) 
            => action.WhenExecuted(e => e.Observe()).PushStackFrame(memberName,filePath,lineNumber);
        
        public static IObservable<T> WhenExecuted<T>(this SimpleAction action, Func<SimpleActionExecuteEventArgs, IObservable<T>> resilientSelector, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => action.When(nameof(ActionBase.Executed), resilientSelector).PushStackFrame(memberName, filePath, lineNumber);
        
        public static IObservable<SimpleActionExecuteEventArgs> WhenExecuted(this SimpleAction action, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => action.WhenExecuted(e => e.Observe()).PushStackFrame(memberName,filePath,lineNumber);
        
        public static IObservable<T> WhenExecuted<T>(this SingleChoiceAction action, Func<SingleChoiceActionExecuteEventArgs, IObservable<T>> resilientSelector, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => action.When(nameof(ActionBase.Executed),resilientSelector).PushStackFrame(memberName,filePath,lineNumber);
        
        public static IObservable<T> WhenExecuted<T>(this ParametrizedAction parametrizedAction, Func<ParametrizedActionExecuteEventArgs, IObservable<T>> resilientSelector,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => parametrizedAction.When(nameof(ActionBase.Executed),resilientSelector).PushStackFrame(memberName,filePath,lineNumber);
        
        public static IObservable<T> WhenExecuted<T>(this IObservable<SimpleAction> source, Func<SimpleActionExecuteEventArgs, IObservable<T>> resilientSelector, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => source.SelectMany(action => action.WhenExecuted(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame(memberName,filePath,lineNumber);
        
        public static IObservable<T> WhenExecuted<T>(this IObservable<SingleChoiceAction> source, Func<SingleChoiceActionExecuteEventArgs, IObservable<T>> resilientSelector, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => source.SelectMany( action => action.WhenExecuted(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame(memberName,filePath,lineNumber);
        
        public static IObservable<T> WhenExecuted<T>(this IObservable<ParametrizedAction> source, Func<ParametrizedActionExecuteEventArgs, IObservable<T>> resilientSelector, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => source.SelectMany(action => action.WhenExecuted(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame(memberName,filePath,lineNumber);
        
        public static IObservable<T> WhenExecuted<T>(this IObservable<PopupWindowShowAction> source, Func<PopupWindowShowActionExecuteEventArgs, IObservable<T>> resilientSelector, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => source.SelectMany(action => action.When(nameof(ActionBase.Executed),resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame(memberName,filePath,lineNumber);

        public static IObservable<SimpleAction> WhenExecuted(this IObservable<SimpleAction> source, Action<SimpleActionExecuteEventArgs> resilientSelector, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => source.WhenExecuted(e => e.DeferAction(() => resilientSelector(e)).To(e.Action).Concat(e.Action.Observe()).Cast<SimpleAction>(),memberName,filePath,lineNumber);
        
        public static IObservable<SingleChoiceAction> WhenExecuted(this IObservable<SingleChoiceAction> source, Action<SingleChoiceActionExecuteEventArgs> resilientSelector, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => source.WhenExecuted(e => e.DeferAction(() => resilientSelector(e)).To(e.Action).Concat(e.Action.Observe()).Cast<SingleChoiceAction>(),memberName,filePath,lineNumber);
        
        public static IObservable<ParametrizedAction> WhenExecuted(this IObservable<ParametrizedAction> source, Action<ParametrizedActionExecuteEventArgs> resilientSelector, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => source.WhenExecuted(e => e.DeferAction(() => resilientSelector(e)).To(e.Action).Concat(e.Action.Observe()).Cast<ParametrizedAction>(),memberName,filePath,lineNumber);

        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecuted(this SingleChoiceAction action) 
            => action.ProcessEvent<SingleChoiceActionExecuteEventArgs>(nameof(SingleChoiceAction.Executed)).TakeUntilDisposed(action);
        
        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecuted(this ParametrizedAction action) 
            => action.ProcessEvent<ParametrizedActionExecuteEventArgs>(nameof(ParametrizedAction.Executed)).TakeUntilDisposed(action);
        
        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecuted(this PopupWindowShowAction action) 
            => action.ProcessEvent<PopupWindowShowActionExecuteEventArgs>(nameof(PopupWindowShowAction.Executed)).TakeUntilDisposed(action);
        
        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecuted(this IObservable<ParametrizedAction> source) 
            => source.SelectMany(action => action.WhenExecuted());
        
        public static IObservable<ActionBaseEventArgs> WhenExecuted<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.SelectMany(a=>a.WhenExecuted());

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecuted(this IObservable<SimpleAction> source) 
            => source.SelectMany(a=>a.WhenExecuted()).Cast<SimpleActionExecuteEventArgs>();
        
        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecuted(this IObservable<SingleChoiceAction> source) 
            => source.SelectMany(a=>a.WhenExecuted()).Cast<SingleChoiceActionExecuteEventArgs>();
        
        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecuted(this IObservable<PopupWindowShowAction> source) 
            => source.SelectMany(a=>a.WhenExecuted()).Cast<PopupWindowShowActionExecuteEventArgs>();
    }
}
