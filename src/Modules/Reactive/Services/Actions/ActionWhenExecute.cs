using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp.Actions;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.Reactive.Services.Actions {
    public static partial class ActionsService {

        public static IObservable<T> WhenExecute<T>(this SimpleAction simpleAction,Func<SimpleActionExecuteEventArgs,IObservable<T>> resilientSelector, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) 
            => simpleAction.ProcessEvent(nameof(SimpleAction.Execute), resilientSelector, context: [simpleAction])
                .TakeUntilDisposed(simpleAction).PushStackFrame(memberName,filePath,lineNumber);        
        public static IObservable<T> WhenExecute<T>(this SingleChoiceAction singleChoiceAction,Func<SingleChoiceActionExecuteEventArgs,IObservable<T>> resilientSelector, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) 
            => singleChoiceAction.ProcessEvent(nameof(SingleChoiceAction.Execute),resilientSelector).TakeUntilDisposed(singleChoiceAction).PushStackFrame(memberName,filePath,lineNumber);

        public static IObservable<T> WhenExecute<T>(this ParametrizedAction parametrizedAction,Func<ParametrizedActionExecuteEventArgs,IObservable<T>> resilientSelector, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) 
            => parametrizedAction.ProcessEvent(nameof(ParametrizedAction.Execute),resilientSelector).TakeUntilDisposed(parametrizedAction).PushStackFrame(memberName,filePath,lineNumber);

        public static IObservable<T> WhenExecute<T>(this PopupWindowShowAction popupWindowShowAction,Func<PopupWindowShowActionExecuteEventArgs,IObservable<T>> resilientSelector, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) 
            => popupWindowShowAction.ProcessEvent(nameof(PopupWindowShowAction.Execute),resilientSelector).TakeUntilDisposed(popupWindowShowAction).PushStackFrame(memberName,filePath,lineNumber);

        public static IObservable<T> WhenExecute<T>(this IObservable<SimpleAction> source,Func<SimpleActionExecuteEventArgs, IObservable<T>> resilientSelector,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) 
            => source.SelectMany(action => action.WhenExecute(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame(memberName,filePath,lineNumber);
        
        public static IObservable<T> WhenExecute<T>(this IObservable<SingleChoiceAction> source,Func<SingleChoiceActionExecuteEventArgs, IObservable<T>> resilientSelector,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) 
            => source.SelectMany(action => action.WhenExecute(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame(memberName,filePath,lineNumber);
        
        public static IObservable<T> WhenExecute<T>(this IObservable<ParametrizedAction> source,Func<ParametrizedActionExecuteEventArgs, IObservable<T>> resilientSelector,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) 
            => source.SelectMany( action => action.WhenExecute(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame(memberName,filePath,lineNumber);
        
        public static IObservable<T> WhenExecute<T>(this IObservable<PopupWindowShowAction> source,Func<PopupWindowShowActionExecuteEventArgs, IObservable<T>> resilientSelector,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) 
            => source.SelectMany(action => action.WhenExecute(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame(memberName,filePath,lineNumber);

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecute(this IObservable<SimpleAction> source) 
            => source.SelectMany(action => action.WhenExecute());
        
        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecute(this IObservable<ParametrizedAction> source) 
            => source.SelectMany(action => action.WhenExecute());
        
        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecute(this IObservable<PopupWindowShowAction> source) 
            => source.SelectMany(action => action.WhenExecute());

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecute(this SimpleAction simpleAction) 
            => simpleAction.ProcessEvent<SimpleActionExecuteEventArgs>(nameof(SimpleAction.Execute)).TakeUntilDisposed(simpleAction);
        
        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecute(this PopupWindowShowAction action) 
            => action.ProcessEvent<PopupWindowShowActionExecuteEventArgs>(nameof(PopupWindowShowAction.Execute)).TakeUntilDisposed(action);
        
        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecute(this ParametrizedAction action) 
            => action.ProcessEvent<ParametrizedActionExecuteEventArgs>(nameof(ParametrizedAction.Execute)).TakeUntilDisposed(action);

        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecute(this SingleChoiceAction singleChoiceAction) 
            => singleChoiceAction.ProcessEvent<SingleChoiceActionExecuteEventArgs>(nameof(SingleChoiceAction.Execute));

        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecute(this IObservable<SingleChoiceAction> source) 
            => source.SelectMany(action => action.WhenExecute());
    }
}