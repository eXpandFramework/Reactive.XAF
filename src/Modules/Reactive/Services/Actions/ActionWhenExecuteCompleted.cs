using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Actions;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services.Actions {
    public static partial class ActionsService {
        public static IObservable<T> WhenExecuteCompleted<T>(this ActionBase action,
            Func<ActionBaseEventArgs, IObservable<T>> resilientSelector)
            => action.When(nameof(ActionBase.ExecuteCompleted), resilientSelector)
                .PushStackFrame();

        public static IObservable<ActionBaseEventArgs> WhenExecuteCompleted(this ActionBase action)
            => action.WhenExecuteCompleted(e => e.Observe());

        public static IObservable<T> WhenExecuteCompleted<T>(this SimpleAction action,
            Func<SimpleActionExecuteEventArgs, IObservable<T>> resilientSelector)
            => ((ActionBase)action).WhenExecuteCompleted(e =>resilientSelector((SimpleActionExecuteEventArgs)e) );

        public static IObservable<T> WhenExecuteCompleted<T>(this SingleChoiceAction action,
            Func<SingleChoiceActionExecuteEventArgs, IObservable<T>> resilientSelector)
            => ((ActionBase)action).WhenExecuteCompleted(e =>resilientSelector((SingleChoiceActionExecuteEventArgs)e) );

        public static IObservable<T> WhenExecuteCompleted<T>(this ParametrizedAction action,
            Func<ParametrizedActionExecuteEventArgs, IObservable<T>> resilientSelector)
            => ((ActionBase)action).WhenExecuteCompleted(e =>resilientSelector((ParametrizedActionExecuteEventArgs)e) );

        public static IObservable<T> WhenExecuteCompleted<T>(this PopupWindowShowAction action,
            Func<PopupWindowShowActionExecuteEventArgs, IObservable<T>> resilientSelector)
            => ((ActionBase)action).WhenExecuteCompleted(e =>resilientSelector((PopupWindowShowActionExecuteEventArgs)e) );

        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecuteCompleted(this SingleChoiceAction action)
            => action.WhenExecuteCompleted(e => e.Observe());

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecuteCompleted(this SimpleAction action)
            => action.WhenExecuteCompleted(e => e.Observe());

        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecuteCompleted(this ParametrizedAction action)
            => action.WhenExecuteCompleted(e => e.Observe());

        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecuteCompleted(this PopupWindowShowAction action)
            => action.WhenExecuteCompleted(e => e.Observe());

        public static IObservable<ActionBaseEventArgs> WhenExecuteCompleted(this IObservable<ActionBase> source)
            => source.SelectMany(a => a.WhenExecuteCompleted());
        public static IObservable<SimpleActionExecuteEventArgs> WhenExecuteCompleted(this IObservable<SimpleAction> source)
            => source.SelectMany(a => a.WhenExecuteCompleted());
        
        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecuteCompleted(this IObservable<SingleChoiceAction> source)
            => source.SelectMany(a => a.WhenExecuteCompleted());
        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecuteCompleted(this IObservable<ParametrizedAction> source)
            => source.SelectMany(a => a.WhenExecuteCompleted());
        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecuteCompleted(this IObservable<PopupWindowShowAction> source)
            => source.SelectMany(a => a.WhenExecuteCompleted());

        
    }
}