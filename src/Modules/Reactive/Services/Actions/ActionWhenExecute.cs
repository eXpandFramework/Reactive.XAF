using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Actions;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.Reactive.Services.Actions {
    public static partial class ActionsService {
        public static IObservable<T> WhenExecute<T>(this ActionBase action,
            Func<ActionBaseEventArgs, IObservable<T>> resilientSelector)
            => action.When("Execute",resilientSelector)
                .PushStackFrame();

        public static IObservable<ActionBaseEventArgs> WhenExecute(this ActionBase action)
            => action.WhenExecuteCompleted(e => e.Observe());

        public static IObservable<T> WhenExecute<T>(this SimpleAction simpleAction,Func<SimpleActionExecuteEventArgs,IObservable<T>> resilientSelector) 
            => ((ActionBase)simpleAction).WhenExecute(e =>resilientSelector((SimpleActionExecuteEventArgs)e) );
        
        public static IObservable<T> WhenExecute<T>(this SingleChoiceAction singleChoiceAction,Func<SingleChoiceActionExecuteEventArgs,IObservable<T>> resilientSelector) 
            => ((ActionBase)singleChoiceAction).WhenExecute(e =>resilientSelector((SingleChoiceActionExecuteEventArgs)e) ) ;

        public static IObservable<T> WhenExecute<T>(this ParametrizedAction parametrizedAction,Func<ParametrizedActionExecuteEventArgs,IObservable<T>> resilientSelector) 
            => ((ActionBase)parametrizedAction).WhenExecute(e =>resilientSelector((ParametrizedActionExecuteEventArgs)e) ) ;

        public static IObservable<T> WhenExecute<T>(this PopupWindowShowAction popupWindowShowAction,Func<PopupWindowShowActionExecuteEventArgs,IObservable<T>> resilientSelector) 
            => ((ActionBase)popupWindowShowAction).WhenExecute(e =>resilientSelector((PopupWindowShowActionExecuteEventArgs)e) ) ;

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecute(this SimpleAction simpleAction) 
            => simpleAction.WhenExecute(e => e.Observe());
        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecute(this PopupWindowShowAction action) 
            => action.WhenExecute(e => e.Observe());
        
        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecute(this ParametrizedAction action) 
            => action.WhenExecute(e => e.Observe());

        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecute(this SingleChoiceAction singleChoiceAction) 
            => singleChoiceAction.WhenExecute(e => e.Observe());
        
        public static IObservable<T> WhenExecute<T>(this IObservable<SimpleAction> source,Func<SimpleActionExecuteEventArgs, IObservable<T>> resilientSelector) 
            => source.SelectMany(action => action.WhenExecute(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame();
        
        public static IObservable<T> WhenExecute<T>(this IObservable<SingleChoiceAction> source,Func<SingleChoiceActionExecuteEventArgs, IObservable<T>> resilientSelector) 
            => source.SelectMany(action => action.WhenExecute(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame();
        
        public static IObservable<T> WhenExecute<T>(this IObservable<ParametrizedAction> source,Func<ParametrizedActionExecuteEventArgs, IObservable<T>> resilientSelector) 
            => source.SelectMany( action => action.WhenExecute(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame();
        
        public static IObservable<T> WhenExecute<T>(this IObservable<PopupWindowShowAction> source,Func<PopupWindowShowActionExecuteEventArgs, IObservable<T>> resilientSelector) 
            => source.SelectMany(action => action.WhenExecute(resilientSelector).TakeUntilDeactivated(action.Controller)).PushStackFrame() ;

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecute(this IObservable<SimpleAction> source) 
            => source.SelectMany(action => action.WhenExecute());
        
        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecute(this IObservable<ParametrizedAction> source) 
            => source.SelectMany(action => action.WhenExecute());
        
        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecute(this IObservable<PopupWindowShowAction> source) 
            => source.SelectMany(action => action.WhenExecute());

        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecute(this IObservable<SingleChoiceAction> source) 
            => source.SelectMany(action => action.WhenExecute());
    }
}