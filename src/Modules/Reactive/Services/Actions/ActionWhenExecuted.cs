using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Actions;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.Reactive.Services.Actions {
    public static partial class ActionsService {
        public static IObservable<T> WhenExecuted<T>(this ActionBase action,
            Func<ActionBaseEventArgs, IObservable<T>> resilientSelector)
            => action.When(nameof(ActionBase.Executed),resilientSelector)
                .PushStackFrame();

        public static IObservable<ActionBaseEventArgs> WhenExecuted(this ActionBase action)
            => action.WhenExecuteCompleted(e => e.Observe());
        
        public static IObservable<T> WhenExecuted<T>(this SimpleAction action, Func<SimpleActionExecuteEventArgs, IObservable<T>> resilientSelector) 
            => ((ActionBase)action).WhenExecuted(e =>resilientSelector((SimpleActionExecuteEventArgs)e) ) ;

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecuted(this SimpleAction action)
            => action.WhenExecuted(e => e.Observe());
        
        public static IObservable<T> WhenExecuted<T>(this SingleChoiceAction action, Func<SingleChoiceActionExecuteEventArgs, IObservable<T>> resilientSelector)
            => ((ActionBase)action).WhenExecuted(e =>resilientSelector((SingleChoiceActionExecuteEventArgs)e) );
        
        public static IObservable<T> WhenExecuted<T>(this ParametrizedAction parametrizedAction, Func<ParametrizedActionExecuteEventArgs, IObservable<T>> resilientSelector)
            => ((ActionBase)parametrizedAction).WhenExecuted(e =>resilientSelector((ParametrizedActionExecuteEventArgs)e) );
        public static IObservable<T> WhenExecuted<T>(this PopupWindowShowAction action, Func<PopupWindowShowActionExecuteEventArgs, IObservable<T>> resilientSelector)
            => ((ActionBase)action).WhenExecuted(e =>resilientSelector((PopupWindowShowActionExecuteEventArgs)e) );
        
        public static IObservable<T> WhenExecuted<T>(this IObservable<SimpleAction> source, Func<SimpleActionExecuteEventArgs, IObservable<T>> resilientSelector) 
            => source.SelectMany(action => action.WhenExecuted(resilientSelector).TakeUntilDeactivated(action.Controller));

        public static IObservable<T> WhenExecuted<T>(this IObservable<SingleChoiceAction> source, Func<SingleChoiceActionExecuteEventArgs, IObservable<T>> resilientSelector)
            => source.SelectMany( action => action.WhenExecuted(resilientSelector).TakeUntilDeactivated(action.Controller)) ;

        public static IObservable<T> WhenExecuted<T>(this IObservable<ParametrizedAction> source, Func<ParametrizedActionExecuteEventArgs, IObservable<T>> resilientSelector)
            => source.SelectMany(action => action.WhenExecuted(resilientSelector).TakeUntilDeactivated(action.Controller)) ;
        
        public static IObservable<T> WhenExecuted<T>(this IObservable<PopupWindowShowAction> source, Func<PopupWindowShowActionExecuteEventArgs, IObservable<T>> resilientSelector)
            => source.SelectMany(action => action.When(nameof(ActionBase.Executed),resilientSelector).TakeUntilDeactivated(action.Controller));

        public static IObservable<SimpleAction> WhenExecuted(this IObservable<SimpleAction> source, Action<SimpleActionExecuteEventArgs> resilientSelector)
            => source.WhenExecuted(e => e.DeferAction(() => resilientSelector(e)).To(e.Action).Concat(e.Action.Observe()).Cast<SimpleAction>()) ;
        
        public static IObservable<SingleChoiceAction> WhenExecuted(this IObservable<SingleChoiceAction> source, Action<SingleChoiceActionExecuteEventArgs> resilientSelector)
            => source.WhenExecuted(e => e.DeferAction(() => resilientSelector(e)).To(e.Action).Concat(e.Action.Observe()).Cast<SingleChoiceAction>()) ;
        
        public static IObservable<ParametrizedAction> WhenExecuted(this IObservable<ParametrizedAction> source, Action<ParametrizedActionExecuteEventArgs> resilientSelector)
            => source.WhenExecuted(e => e.DeferAction(() => resilientSelector(e)).To(e.Action).Concat(e.Action.Observe()).Cast<ParametrizedAction>()) ;

        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecuted(this SingleChoiceAction action) 
            => action.WhenExecuted(e =>e.Observe() ) ;
        
        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecuted(this ParametrizedAction action) 
            => action.WhenExecuted(e =>e.Observe() ) ;
        
        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecuted(this PopupWindowShowAction action) 
            => action.WhenExecuted(e =>e.Observe() ) ;
        
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