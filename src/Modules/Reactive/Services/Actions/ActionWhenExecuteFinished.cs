using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp.Actions;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services.Actions {
    public static partial class ActionsService {
        public static IObservable<TAction> WhenExecuteFinished<TAction>(this TAction action,bool customEmit=false) where TAction : ActionBase
            => customEmit || (action.Data.ContainsKey(nameof(ExecutionFinished)) && (bool)action.Data[nameof(ExecutionFinished)])
                ? ExecuteFinishedSubject.Where(a => a == action).Cast<TAction>() 
                : action.WhenExecuteCompleted().To(action);

        public static IObservable<TAction> WhenExecuteFinished<TAction>(this IObservable<TAction> source,
            bool customEmit = false) where TAction : ActionBase
            => source.SelectMany(a => a.WhenExecuteFinished(customEmit))
                .PushStackFrame();

        private static readonly ISubject<ActionBase> ExecuteFinishedSubject = Subject.Synchronize(new Subject<ActionBase>());
        
        public static void CustomizeExecutionFinished<TAction>(this TAction action, bool enable=true) where TAction : ActionBase
            => action.Data[nameof(ExecutionFinished)] = enable;
        
        public static void ExecutionFinished<TAction>(this TAction action) where TAction:ActionBase 
            => ExecuteFinishedSubject.OnNext(action);
    }
}