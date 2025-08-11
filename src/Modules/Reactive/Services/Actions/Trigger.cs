using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;

namespace Xpand.XAF.Modules.Reactive.Services.Actions {
    public static partial class ActionsService {
        public static IObservable<T> Trigger<T>(this SingleChoiceAction action, IObservable<T> afterExecuted,params object[] selection)
            => action.Trigger(afterExecuted,() => action.SelectedItem,selection);
        
        public static IObservable<Unit> Trigger(this SingleChoiceAction action, params object[] selection)
            => action.Trigger(Observable.Empty<Unit>(),() => action.SelectedItem??action.Items.FirstOrDefault(),selection);
        public static IObservable<Unit> Trigger(this SingleChoiceAction action, Func<ChoiceActionItem> selectedItem)
            => action.Trigger(Observable.Empty<Unit>(),selectedItem);
        
        public static IObservable<T> Trigger<T>(this PopupWindowShowAction action, IObservable<T> afterExecuted) 
            => action.ShowPopupWindow().ToController<DialogController>().DelayOnContext()
                .SelectMany(controller => controller.AcceptAction.Trigger(afterExecuted));

        public static IObservable<T> Trigger<T>(this SingleChoiceAction action,Func<ChoiceActionItem> selectedItem, IObservable<T> afterExecuted,params object[] selection)
            => afterExecuted.Trigger(() => action.DoExecute(selectedItem(), selection));

        private static IObservable<T> Trigger<T>(this IObservable<T> afterExecuted, Action action)
            => afterExecuted.Merge(Observable.Defer(() => {
                action();
                return Observable.Empty<T>();
            }),new SynchronizationContextScheduler(SynchronizationContext.Current!));

        public static IObservable<T> Trigger<T>(this ParametrizedAction action, IObservable<T> afterExecuted)
            => afterExecuted.Trigger(() => action.DoExecute(action.Value));
        
        public static IObservable<T> Trigger<T>(this SimpleAction action, IObservable<T> afterExecuted,params object[] selection)
            => afterExecuted.Trigger(() => action.DoExecute(selection));

        public static IObservable<Unit> Trigger(this ActionBase action)
            => action switch{
                SimpleAction simpleAction => simpleAction.Trigger(),
                SingleChoiceAction singleChoiceAction => singleChoiceAction.Trigger(),
                ParametrizedAction parametrizedAction => parametrizedAction.Trigger(Observable.Empty<Unit>()),
                _ => throw new NotImplementedException(action.ToString())
            };

        public static IObservable<Unit> Trigger(this SimpleAction action, params object[] selection)
            => action.Trigger(action.WhenExecuteCompleted().ToUnit(),selection);
        public static IObservable<T> Trigger<T>(this SingleChoiceAction action, IObservable<T> afterExecuted,
            Func<ChoiceActionItem> selectedItem, params object[] selection)
            => afterExecuted.Trigger(() => action.DoExecute(selectedItem(), selection));
        
        public static IObservable<ParametrizedAction> WhenValueChangedTrigger(this IObservable<ParametrizedAction> source)
            => source.WhenValueChangedApplyValue(action => action.Trigger());
        
        public static IObservable<Unit> Trigger<TAction>(this IObservable<TAction> source) where TAction : ActionBase
            => source.SelectMany(action => action.Trigger());
        
        public static IObservable<TAction> TriggerWhenActivated<TAction>(this IObservable<TAction> source) where TAction:ActionBase 
            => source.MergeIgnored(@base => @base.Observe().WhenControllerActivated(action => action.Trigger()));
    }
}