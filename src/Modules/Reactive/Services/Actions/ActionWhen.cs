using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Actions;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services.Actions {
    public static partial class ActionsService {
        public static IObservable<T> When<TEventArgs, T>(this ActionBase action, string eventName,
            Func<TEventArgs, IObservable<T>> resilientSelector) where TEventArgs : EventArgs
            => action.ProcessEvent(eventName, resilientSelector, context: [action])
                .TakeUntilDisposed(action) ;

        public static IObservable<T> When<TEventArgs, T>(this IObservable<ActionBase> source, string eventName,
            Func<TEventArgs, IObservable<T>> resilientSelector) where TEventArgs : EventArgs
            => source.SelectMany(a => a.When(eventName, resilientSelector)).PushStackFrame()
                .PushStackFrame();

        
        public static IObservable<TEventArgs> When<TEventArgs>(this ActionBase action,string eventName) 
            where TEventArgs : EventArgs 
            => action.ProcessEvent<TEventArgs>(eventName).TakeUntilDisposed(action);
        
        public static IObservable<TAction> When<TAction>(this IObservable<TAction> source, Type objectType) where TAction : ActionBase 
            => source.Where(action => objectType.IsAssignableFrom(action.Controller.Frame.View.ObjectTypeInfo.Type));
    }
}