using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Fasterflect;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.Reactive.Services.Actions {
    public static partial class ActionsService {

        public static IObservable<T> WhenExecuting<TAction, T>(this TAction action, Func<CancelEventArgs, IObservable<T>> resilientSelector) where TAction : ActionBase
            => action.ProcessEvent<CancelEventArgs, T>(nameof(ActionBase.Executing), e => resilientSelector(e).DoOnError(_ => e.Cancel = true), [action]).TakeUntilDisposed(action) ;        
        public static IObservable<T2> WhenUpdating<T2>(this ActionBase action,Func<UpdateActionEventArgs,IObservable<T2>> selector) 
            => action.Controller.WhenActivated(true)
                .SelectManyUntilDeactivated(controller => controller.Frame.GetController<ActionsCriteriaViewController>()
                    .ProcessEvent<UpdateActionEventArgs>(nameof(ActionsCriteriaViewController.ActionUpdating)).Where(e => e.Active&&e.NeedUpdateEnabled)
                    .SelectMany(selector)) ;
        
        public static IObservable<ParametrizedAction> WhenValueChangedApplyValue(this IObservable<ParametrizedAction> source,Func<ParametrizedAction,IObservable<Unit>> selector=null)
            => source.WhenCustomizeControl(t => t.e.Control.Observe()
                .SelectMany(spinEdit => spinEdit.ProcessEvent("ValueChanged")
                    .Select(_ => spinEdit.GetPropertyValue("EditValue")).WhenNotDefault()
                    .Do(value =>t.action.Value=value )
                    .WaitUntilInactive(TimeSpan.FromSeconds(1)).ObserveOnContext().To(t.action)
                    .SelectMany(action => selector?.Invoke(action)??Observable.Empty<Unit>())));
        

        public static IObservable<(TAction action, CancelEventArgs e)> WhenCanceled<TAction>(
            this IObservable<(TAction action, CancelEventArgs e)> source) where TAction : ActionBase
            => source.Where(t => t.e.Cancel);
        
        public static IObservable<(TAction action, CancelEventArgs e)> WhenNotCanceled<TAction>(
            this IObservable<(TAction action, CancelEventArgs e)> source) where TAction : ActionBase
            => source.Where(t => !t.e.Cancel);

        public static IObservable<CustomizePopupWindowParamsEventArgs> WhenCustomizePopupWindowParams(this PopupWindowShowAction action) 
            => action.ProcessEvent<CustomizePopupWindowParamsEventArgs>(nameof(PopupWindowShowAction.CustomizePopupWindowParams))
                .TakeUntilDisposed(action);

        public static IObservable<ItemsChangedEventArgs> WhenItemsChanged(this SingleChoiceAction action,ChoiceActionItemChangesType? changesType=null) 
            => action.ProcessEvent<ItemsChangedEventArgs>(nameof(SingleChoiceAction.ItemsChanged))
                .Where(e =>changesType==null||e.ChangedItemsInfo.Any(pair => pair.Value==changesType) ).TakeUntil(action.WhenDisposed());

        public static IObservable<Unit> WhenDisposing<TAction>(this TAction simpleAction) where TAction : ActionBase 
            => simpleAction.ProcessEvent(nameof(ActionBase.Disposing)).ToUnit();
        
        public static IObservable<TAction> WhenChanged<TAction>(this IObservable<TAction> source,ActionChangedType? actionChangedType = null)where TAction : ActionBase 
            => source.SelectMany(a => a.WhenChanged(actionChangedType));

        public static IObservable<TAction> WhenChanged<TAction>(this TAction action, ActionChangedType? actionChangedType = null) where TAction : ActionBase 
            => action.ProcessEvent<ActionChangedEventArgs>(nameof(ActionBase.Changed))
                .Where(e => actionChangedType == null || e.ChangedPropertyType == actionChangedType)
                .To(action);
        
        public static IObservable<Unit> Disposing<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source .SelectMany(item => item.ProcessEvent(nameof(ActionBase.Disposing)).ToUnit());

        public static IObservable<ParametrizedAction> WhenValueChanged(this ParametrizedAction action)
            => action.ProcessEvent(nameof(ParametrizedAction.ValueChanged)).TakeUntilDisposed(action) ;
        
        public static IObservable<(TAction action, CustomizeControlEventArgs e)> WhenCustomizeControl<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.SelectMany(a => a.WhenCustomizeControl().InversePair(a))
                .PushStackFrame();
        
        public static IObservable<TAction> WhenCustomizeControl<TAction>(this IObservable<TAction> source,Func<(TAction action,CustomizeControlEventArgs e),IObservable<Unit>> selector) where TAction : ActionBase 
            => source.MergeIgnored(a => a.WhenCustomizeControl().InversePair(a).SelectMany(selector));

        public static IObservable<CustomizeControlEventArgs> WhenCustomizeControl<TAction>(this TAction action) where TAction:ActionBase 
            => action.ProcessEvent<CustomizeControlEventArgs>(nameof(ActionBase.CustomizeControl));
        
        public static IObservable<CustomizeTemplateEventArgs> WhenCustomizeTemplate(this PopupWindowShowAction action) 
            => action.ProcessEvent<CustomizeTemplateEventArgs>(nameof(action.CustomizeTemplate)).TakeUntilDisposed(action);
    }
}