using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Actions;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services.Actions {
    public static partial class ActionsService {
        public static IObservable<T> WhenExecuteCompleted<T>(this SimpleAction action,Func<SimpleActionExecuteEventArgs, IObservable<T>> resilientSelector)  
            => action.ProcessEvent(nameof(ActionBase.ExecuteCompleted),resilientSelector).TakeUntilDisposed(action).PushStackFrame()
                .PushStackFrame();
        
        public static IObservable<T> WhenExecuteCompleted<T>(this SingleChoiceAction action,Func<SingleChoiceActionExecuteEventArgs, IObservable<T>> resilientSelector)  
            => action.ProcessEvent(nameof(ActionBase.ExecuteCompleted),resilientSelector).TakeUntilDisposed(action).PushStackFrame()
                .PushStackFrame();
        
        public static IObservable<T> WhenExecuteCompleted<T>(this ParametrizedAction action,Func<ParametrizedActionExecuteEventArgs, IObservable<T>> resilientSelector)  
            => action.ProcessEvent(nameof(ActionBase.ExecuteCompleted),resilientSelector).TakeUntilDisposed(action).PushStackFrame()
                .PushStackFrame();
        
        public static IObservable<T> WhenExecuteCompleted<T>(this PopupWindowShowAction action,Func<PopupWindowShowActionExecuteEventArgs, IObservable<T>> resilientSelector)  
            => action.ProcessEvent(nameof(ActionBase.ExecuteCompleted),resilientSelector).TakeUntilDisposed(action).PushStackFrame()
                .PushStackFrame();

        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecuteCompleted(this SingleChoiceAction action) 
            => action.ProcessEvent<SingleChoiceActionExecuteEventArgs>(nameof(ActionBase.ExecuteCompleted)).TakeUntilDisposed(action);
        
        public static IObservable<SimpleActionExecuteEventArgs> WhenExecuteCompleted(this SimpleAction action) 
            => action.ProcessEvent<SimpleActionExecuteEventArgs>(nameof(ActionBase.ExecuteCompleted)).TakeUntilDisposed(action);

        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecuteCompleted(this ParametrizedAction action) 
            => action.ProcessEvent<ParametrizedActionExecuteEventArgs>(nameof(ActionBase.ExecuteCompleted)).TakeUntilDisposed(action);
        
        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecuteCompleted(this PopupWindowShowAction action) 
            => action.ProcessEvent<PopupWindowShowActionExecuteEventArgs>(nameof(ActionBase.ExecuteCompleted)).TakeUntilDisposed(action);
        
        public static IObservable<ActionBaseEventArgs> WhenExecuteCompleted(this IObservable<ActionBase> source) 
            => source.SelectMany(a => a.WhenExecuteCompleted());

        public static IObservable<ActionBaseEventArgs> WhenExecuteCompleted(this ActionBase action)
            => action.ProcessEvent<ActionBaseEventArgs>(nameof(ActionBase.ExecuteCompleted)).TakeUntilDisposed(action);
    }
}