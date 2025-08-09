using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Utils;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.CriteriaOperatorExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.Reactive.Services.Actions{
    
    public static partial class ActionsService {
        public static IObservable<T> WhenAvailable<T>(this IObservable<T> source) where T:ActionBase 
            => source.Where(action => action.Available());

        public static IObservable<T> SetImage<T,TObject>(this IObservable<T> source, CommonImage startImage,
            CommonImage replaceImage, Expression<Func<TObject, bool>> lambda) where T : ActionBase
            => source.MergeIgnored(action => action.SetImage(startImage, replaceImage, lambda));
        
        public static IObservable<T> SetImage<T>(this ActionBase action, CommonImage startImage,
            CommonImage replaceImage, Expression<Func<T, bool>> lambda)  
            => action.Controller.WhenActivated().Do(_ => action.SetImage( lambda,startImage, replaceImage)).Select(_ => action.View())
                .SelectMany(view => view.ObjectSpace.WhenModifiedObjects<T>()
                    .Merge(view.WhenSelectionChanged().SelectMany(_ => view.SelectedObjects.Cast<T>()))
                    .StartWith(view.SelectedObjects.Cast<T>())
                    .WaitUntilInactive(TimeSpan.FromMilliseconds(250)).ObserveOnContext()
                .Do(_ => action.SetImage( lambda,startImage, replaceImage)));

        private static void SetImage<T>(this ActionBase action, Expression<Func<T, bool>> lambda, CommonImage startImage, CommonImage replaceImage) 
            => action.SetImage(action.View().ObjectSpace.IsObjectFitForCriteria(lambda.ToCriteria(),
                action.View().SelectedObjects.Cast<object>().ToArray()) ? replaceImage : startImage);

        public static void Activate(this ActionBase action,string key,bool value){
			action.BeginUpdate();
			action.Active.BeginUpdate();
			action.Active[key] = value;
			action.Active.EndUpdate();
			action.EndUpdate();
        }
        
	    public static IObservable<(TModule module, Frame frame)> Action<TModule>(
		    this IObservable<Frame> source) where TModule : ModuleBase 
            => source.Select(frame => frame.Action<TModule>());

        public static IObservable<T> When<TEventArgs, T>(this IObservable<ActionBase> source, string eventName,
            Func<TEventArgs, IObservable<T>> resilientSelector, [CallerMemberName] string caller = "") where TEventArgs : EventArgs
            => source.SelectMany(a => a.When(eventName, resilientSelector, caller));
        
        public static IObservable<T> When<TEventArgs, T>(this ActionBase action,string eventName, Func<TEventArgs, IObservable<T>> resilientSelector, [CallerMemberName] string caller = "") 
            where TEventArgs : EventArgs 
            => action.ProcessEvent(eventName, resilientSelector,context:[action], caller: caller).TakeUntilDisposed(action);
        public static IObservable<TEventArgs> When<TEventArgs>(this ActionBase action,string eventName, [CallerMemberName] string caller = "") 
            where TEventArgs : EventArgs 
            => action.ProcessEvent<TEventArgs,TEventArgs>(eventName,e => e.Observe(),context:[action] , caller: caller).TakeUntilDisposed(action);

        public static IObservable<T> WhenExecuted<T>(this SimpleAction action, Func<SimpleActionExecuteEventArgs, IObservable<T>> resilientSelector, [CallerMemberName] string caller = "")
            => action.When(nameof(ActionBase.Executed),resilientSelector, caller);
        
        public static IObservable<SimpleActionExecuteEventArgs> WhenExecuted(this SimpleAction action, [CallerMemberName] string caller = "")
            => action.WhenExecuted(e => e.Observe(),caller);
        
        public static IObservable<T> WhenExecuteCompleted<T>(this ParametrizedAction action,Func<ParametrizedActionExecuteEventArgs, IObservable<T>> resilientSelector,[CallerMemberName]string caller="")  
            => action.When(nameof(ActionBase.ExecuteCompleted),resilientSelector, caller);
        public static IObservable<T> WhenExecuteCompleted<T>(this SingleChoiceAction action,Func<SingleChoiceActionExecuteEventArgs, IObservable<T>> resilientSelector,[CallerMemberName]string caller="")  
            => action.When(nameof(ActionBase.ExecuteCompleted),resilientSelector, caller);
        
        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecuteCompleted(this SingleChoiceAction action) 
            => action.ProcessEvent<SingleChoiceActionExecuteEventArgs>(nameof(ActionBase.ExecuteCompleted)).TakeUntilDisposed(action);
        public static IObservable<SimpleActionExecuteEventArgs> WhenExecuteCompleted(this SimpleAction action) 
            => action.ProcessEvent<SimpleActionExecuteEventArgs>(nameof(ActionBase.ExecuteCompleted)).TakeUntilDisposed(action);
        public static IObservable<ActionBaseEventArgs> WhenExecuteCompleted(this ActionBase action) 
            => action.ProcessEvent<ActionBaseEventArgs>(nameof(ActionBase.ExecuteCompleted)).TakeUntilDisposed(action);
        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecuteCompleted(this ParametrizedAction action) 
            => action.ProcessEvent<ParametrizedActionExecuteEventArgs>(nameof(ActionBase.ExecuteCompleted)).TakeUntilDisposed(action);
        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecuteCompleted(this PopupWindowShowAction action) 
            => action.ProcessEvent<PopupWindowShowActionExecuteEventArgs>(nameof(ActionBase.ExecuteCompleted)).TakeUntilDisposed(action);
        
        public static IObservable<T> WhenExecuteCompleted<T>(this SimpleAction action,Func<SimpleActionExecuteEventArgs, IObservable<T>> resilientSelector,[CallerMemberName]string caller="")  
            => action.When(nameof(ActionBase.ExecuteCompleted),resilientSelector, caller);
        
        public static IObservable<T> WhenExecuteCompleted<T>(this PopupWindowShowAction action,Func<PopupWindowShowActionExecuteEventArgs, IObservable<T>> resilientSelector,[CallerMemberName]string caller="")  
            => action.When(nameof(ActionBase.ExecuteCompleted),resilientSelector, caller);
        
        
        
        public static IObservable<T> WhenExecuted<T>(this SingleChoiceAction action, Func<SingleChoiceActionExecuteEventArgs, IObservable<T>> resilientSelector, [CallerMemberName] string caller = "")
            => action.When(nameof(ActionBase.Executed),resilientSelector, caller);
        public static IObservable<T> WhenExecuted<T>(this ParametrizedAction parametrizedAction, Func<ParametrizedActionExecuteEventArgs, IObservable<T>> resilientSelector,[CallerMemberName]string caller="")
            => parametrizedAction.When(nameof(ActionBase.Executed),resilientSelector, caller);
        public static IObservable<T> WhenExecuted<TAction,TArg,T>(this TAction parametrizedAction, Func<TArg, IObservable<T>> resilientSelector,[CallerMemberName]string caller="") where TAction : ActionBase where TArg : ActionBaseEventArgs 
            => parametrizedAction.When(nameof(ActionBase.Executed),resilientSelector, caller);
        public static IObservable<T> WhenExecuted<TAction,TArg,T>(this IObservable<TAction> source, Func<TArg, IObservable<T>> resilientSelector,[CallerMemberName]string caller="") where TAction : ActionBase where TArg : ActionBaseEventArgs 
            => source.SelectMany(action => action.When(nameof(ActionBase.Executed),resilientSelector, caller));
        
        public static IObservable<T> WhenExecuted<T>(this IObservable<SimpleAction> source, Func<SimpleActionExecuteEventArgs, IObservable<T>> resilientSelector, [CallerMemberName] string caller = "")
            => source.SelectMany(action => action.WhenExecuted(resilientSelector,caller).TakeUntilDeactivated(action.Controller));
        public static IObservable<SimpleAction> WhenExecuted(this IObservable<SimpleAction> source, Action<SimpleActionExecuteEventArgs> resilientSelector, [CallerMemberName] string caller = "")
            => source.WhenExecuted(e => e.DeferAction(() => resilientSelector(e)).To(e.Action).Concat(e.Action.Observe()).Cast<SimpleAction>(),caller);
        public static IObservable<SingleChoiceAction> WhenExecuted(this IObservable<SingleChoiceAction> source, Action<SingleChoiceActionExecuteEventArgs> resilientSelector, [CallerMemberName] string caller = "")
            => source.WhenExecuted(e => e.DeferAction(() => resilientSelector(e)).To(e.Action).Concat(e.Action.Observe()).Cast<SingleChoiceAction>(),caller);
        public static IObservable<ParametrizedAction> WhenExecuted(this IObservable<ParametrizedAction> source, Action<ParametrizedActionExecuteEventArgs> resilientSelector, [CallerMemberName] string caller = "")
            => source.WhenExecuted(e => e.DeferAction(() => resilientSelector(e)).To(e.Action).Concat(e.Action.Observe()).Cast<ParametrizedAction>(),caller);
        
        public static IObservable<T> WhenConcatExecution<T>(this IObservable<SimpleAction> source, Func<SimpleActionExecuteEventArgs, IObservable<T>> resilientSelector,[CallerMemberName]string caller="")
            => source.SelectMany(action => action.WhenConcatExecution(resilientSelector,caller:caller).TakeUntilDeactivated(action.Controller));
        public static IObservable<SimpleAction> WhenConcatExecution(this IObservable<SimpleAction> source, Action<SimpleActionExecuteEventArgs> resilientSelector,[CallerMemberName]string caller="")
            => source.WhenConcatExecution(e => e.DeferAction(() => resilientSelector(e)).To<SimpleAction>().Concat(e.Action.Observe().Cast<SimpleAction>()),caller);
        public static IObservable<SingleChoiceAction> WhenConcatExecution(this IObservable<SingleChoiceAction> source, Action<SingleChoiceActionExecuteEventArgs> resilientSelector,[CallerMemberName]string caller="")
            => source.WhenConcatExecution(e => e.DeferAction(() => resilientSelector(e)).To<SingleChoiceAction>().Concat(e.Action.Observe().Cast<SingleChoiceAction>()),caller);
        public static IObservable<ParametrizedAction> WhenConcatExecution(this IObservable<ParametrizedAction> source, Action<ParametrizedActionExecuteEventArgs> resilientSelector,[CallerMemberName]string caller="")
            => source.WhenConcatExecution(e => e.DeferAction(() => resilientSelector(e)).To<ParametrizedAction>().Concat(e.Action.Observe().Cast<ParametrizedAction>()),caller);
        
        public static IObservable<T> WhenExecute<T>(this IObservable<SimpleAction> source,Func<SimpleActionExecuteEventArgs, IObservable<T>> resilientSelector,[CallerMemberName]string caller="") 
            => source.SelectMany(action => action.WhenExecuted(resilientSelector, caller).TakeUntilDeactivated(action.Controller));
        
        
        public static IObservable<T> WhenExecuted<T>(this IObservable<SingleChoiceAction> source, Func<SingleChoiceActionExecuteEventArgs, IObservable<T>> resilientSelector, [CallerMemberName] string caller = "")
            => source.SelectMany(action => action.WhenExecuted(resilientSelector, caller).TakeUntilDeactivated(action.Controller));
        public static IObservable<T> WhenExecute<T>(this IObservable<SingleChoiceAction> source,Func<SingleChoiceActionExecuteEventArgs, IObservable<T>> resilientSelector,[CallerMemberName]string caller="") 
            => source.SelectMany(action => action.WhenExecuted(resilientSelector, caller).TakeUntilDeactivated(action.Controller));
        
        public static IObservable<T> WhenExecuted<T>(this IObservable<ParametrizedAction> source, Func<ParametrizedActionExecuteEventArgs, IObservable<T>> resilientSelector, [CallerMemberName] string caller = "")
            => source.SelectMany(action => action.WhenExecuted(resilientSelector, caller).TakeUntilDeactivated(action.Controller));
        
        public static IObservable<T> WhenExecute<T>(this IObservable<ParametrizedAction> source,Func<ParametrizedActionExecuteEventArgs, IObservable<T>> resilientSelector,[CallerMemberName]string caller="") 
            => source.SelectMany(action => action.WhenExecuted(resilientSelector, caller).TakeUntilDeactivated(action.Controller));
        
        public static IObservable<T> WhenExecuted<T>(this IObservable<PopupWindowShowAction> source, Func<PopupWindowShowActionExecuteEventArgs, IObservable<T>> resilientSelector, [CallerMemberName] string caller = "")
            => source.SelectMany(action => action.When(nameof(ActionBase.Executed),resilientSelector, caller).TakeUntilDeactivated(action.Controller));

        public static IObservable<T> WhenExecute<T>(this IObservable<PopupWindowShowAction> source,Func<PopupWindowShowActionExecuteEventArgs, IObservable<T>> resilientSelector,[CallerMemberName]string caller="") 
            => source.SelectMany(action => action.When(nameof(ActionBase.Executed),resilientSelector, caller).TakeUntilDeactivated(action.Controller));

        public static IObservable<T> WhenConcatExecution<TArg,T>(this IObservable<ActionBase> source,Func<TArg,IObservable<T>> resilientSelector,[CallerMemberName]string caller="") where TArg:ActionBaseEventArgs
            => source.SelectMany(a => a.WhenConcatExecution(resilientSelector,caller));
        
        public static IObservable<T> WhenConcatExecution<T>(this ParametrizedAction simpleAction,Func<ParametrizedActionExecuteEventArgs,IObservable<T>> resilientSelector,[CallerMemberName]string caller="")
            => simpleAction.WhenConcatExecution<T,ParametrizedActionExecuteEventArgs>( resilientSelector,caller:caller);
        
        public static IObservable<T> WhenConcatExecution<T>(this SingleChoiceAction simpleAction,Func<SingleChoiceActionExecuteEventArgs,IObservable<T>> resilientSelector,[CallerMemberName]string caller="")
            => simpleAction.WhenConcatExecution<T,SingleChoiceActionExecuteEventArgs>( resilientSelector,caller);
        public static IObservable<T> WhenConcatExecution<T>(this SimpleAction simpleAction,Func<SimpleActionExecuteEventArgs,IObservable<T>> resilientSelector,[CallerMemberName]string caller="")
            => simpleAction.WhenConcatExecution<T,SingleChoiceActionExecuteEventArgs>( resilientSelector,caller);
        
        private static IObservable<T> WhenConcatExecution<T, TArgs>(this ActionBase action, Func<TArgs, IObservable<T>> resilientSelector, [CallerMemberName] string caller = "") where TArgs : ActionBaseEventArgs 
            => action.WhenExecuted(e => {
                e.Action.Enabled[nameof(WhenConcatExecution)] = false;
                return resilientSelector((TArgs)e)
                    .ObserveOnContext() 
                    .Finally(() => {
                        e.Action.ExecutionFinished();
                        e.Action.Enabled[nameof(WhenConcatExecution)] = true;
                    });
            }, caller);

        public static IObservable<T> CommitChanges<T>(this IObservable<T> source,[CallerMemberName]string caller="") where T : ActionBaseEventArgs
            => source.SelectMany(e => e.Observe().Do(_ => {
                var view = e.Action.View();
                view?.AsObjectView()?.ObjectSpace.CommitChanges();
            } ).ChainFaultContext([caller, e.Action]));

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecute(this IObservable<SimpleAction> source) 
            => source.SelectMany(action => action.WhenExecute());
        
        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecute(this IObservable<ParametrizedAction> source) 
            => source.SelectMany(action => action.WhenExecute());
        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecuted(this IObservable<ParametrizedAction> source) 
            => source.SelectMany(action => action.WhenExecuted());
        
        public static IObservable<T> WhenConcatExecution<T>(this IObservable<ParametrizedAction> source,Func<ParametrizedActionExecuteEventArgs,IObservable<T>> resilientSelector,[CallerMemberName]string caller="") 
            => source.SelectMany(action => action.WhenConcatExecution(resilientSelector,caller:caller));
        
        public static IObservable<T> WhenConcatExecution<T>(this IObservable<SingleChoiceAction> source,Func<SingleChoiceActionExecuteEventArgs,IObservable<T>> resilientSelector,[CallerMemberName]string caller="") 
            => source.SelectMany(action => action.WhenConcatExecution(resilientSelector,caller:caller));
        public static IObservable<T> WhenConcatExecution<T>(this IObservable<PopupWindowShowAction> source,Func<PopupWindowShowActionExecuteEventArgs,IObservable<T>> resilientSelector,[CallerMemberName]string caller="") 
            => source.SelectMany(action => action.WhenConcatExecution(resilientSelector,caller:caller));
        
        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecute(this IObservable<PopupWindowShowAction> source) 
            => source.SelectMany(action => action.WhenExecute());

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecute(this SimpleAction simpleAction) 
            => simpleAction.ProcessEvent<SimpleActionExecuteEventArgs>(nameof(SimpleAction.Execute)).TakeUntilDisposed(simpleAction);
        public static IObservable<T> WhenExecute<T>(this SimpleAction simpleAction,Func<SimpleActionExecuteEventArgs,IObservable<T>> resilientSelector) 
            => simpleAction.ProcessEvent(nameof(SimpleAction.Execute),resilientSelector).TakeUntilDisposed(simpleAction);
        
        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecute(this PopupWindowShowAction action) 
            => action.ProcessEvent<PopupWindowShowActionExecuteEventArgs>(nameof(PopupWindowShowAction.Execute)).TakeUntilDisposed(action);
        
        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecute(this ParametrizedAction action) 
            => action.ProcessEvent<ParametrizedActionExecuteEventArgs>(nameof(ParametrizedAction.Execute)).TakeUntilDisposed(action);

        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecute(this SingleChoiceAction singleChoiceAction) 
            => singleChoiceAction.ProcessEvent<SingleChoiceActionExecuteEventArgs>(nameof(SingleChoiceAction.Execute));

        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecute(this IObservable<SingleChoiceAction> source) 
            => source.SelectMany(action => action.WhenExecute());

        public static IObservable<TFrame> WhenView<TFrame>(this IObservable<TFrame> source, Type objectType) where TFrame : Frame 
            => source.SelectMany(frame => frame.View.Observe().When(objectType).Select(_ => frame));

        public static IObservable<TAction> When<TAction>(this IObservable<TAction> source, Type objectType) where TAction : ActionBase 
            => source.Where(action => objectType.IsAssignableFrom(action.Controller.Frame.View.ObjectTypeInfo.Type));

        public static IObservable<IObjectSpace> ToObjectSpace<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.Select(action => action.Controller.Frame.View.ObjectSpace);

        public static IObservable<(TAction action, CancelEventArgs e)> WhenCanceled<TAction>(
            this IObservable<(TAction action, CancelEventArgs e)> source) where TAction : ActionBase
            => source.Where(t => t.e.Cancel);
        
        public static IObservable<(TAction action, CancelEventArgs e)> WhenNotCanceled<TAction>(
            this IObservable<(TAction action, CancelEventArgs e)> source) where TAction : ActionBase
            => source.Where(t => !t.e.Cancel);
        
        public static IObservable<T> WhenExecuting<TAction, T>(this TAction action, Func<CancelEventArgs, IObservable<T>> resilientSelector) where TAction : ActionBase
            => action.ProcessEvent< CancelEventArgs,T>(nameof(ActionBase.Executing), e => resilientSelector(e).DoOnError(_ => e.Cancel = true))
                .TakeUntilDisposed(action);

        public static  IObservable<(TAction action, Type objectType, View view, Frame frame, IObjectSpace objectSpace, ShowViewParameters showViewParameters)> ToParameter<TAction>(
                this IObservable<(TAction action, ActionBaseEventArgs e)> source) where TAction : ActionBase => source.Select(t => {
		        var frame = t.action.Controller.Frame;
		        return (t.action, frame.View.ObjectTypeInfo.Type, frame.View, frame, frame.View.ObjectSpace, t.e.ShowViewParameters);
	        });

        public static IObservable<TAction> ToAction<TAction>(this IObservable<(TAction action, ActionBaseEventArgs e)> source) where TAction : ActionBase 
            => source.Select(t => t.action);

        public static IObservable<CustomizePopupWindowParamsEventArgs> WhenCustomizePopupWindowParams(this PopupWindowShowAction action) 
            => action.ProcessEvent<CustomizePopupWindowParamsEventArgs>(nameof(PopupWindowShowAction.CustomizePopupWindowParams))
                .TakeUntilDisposed(action);

        public static IObservable<SingleChoiceAction> AddItems(this IObservable<SingleChoiceAction> source,Func<SingleChoiceAction,IObservable<Unit>> addItems,IScheduler scheduler=null,[CallerMemberName]string caller="")
            => source.MergeIgnored(action => action.Controller.WhenActivated(emitWhenActive: true)
                .SelectMany(_ => action.View().WhenCurrentObjectChanged().StartWith(action.View()).TakeUntilDisposed(action))
                .WaitUntilInactive(1, scheduler: scheduler).ObserveOnContextMaybe()
                .Do(_ => action.Items.Clear()).SelectMany(_ => addItems(action).ChainFaultContext([caller,action])).TakeUntilDisposed(action));

        public static IObservable<TArgs> CreateDetailView<TArgs>(this IObservable<TArgs> source, Type objectType=null, TargetWindow? targetWindow =null) where TArgs:ActionBaseEventArgs
	        => source.Do(e => {
		        var parameters = e.ShowViewParameters;
                objectType ??= e.Action.View().ObjectTypeInfo.Type;
		        parameters.CreatedView = e.Action.Application.NewDetailView(objectType);
                parameters.CreatedView.CurrentObject = parameters.CreatedView.ObjectSpace.CreateObject(objectType);
		        if (targetWindow.HasValue) {
			        parameters.TargetWindow = targetWindow.Value;
		        }
	        });

        
        public static IObservable<T> WhenExecuted<T>(this ActionBase action,Func<ActionBaseEventArgs,IObservable<T>> resilientSelector,[CallerMemberName]string caller="") 
            => action.ProcessEvent(nameof(ActionBase.Executed),resilientSelector,caller:caller).TakeUntilDisposed(action);
        public static IObservable<ActionBaseEventArgs> WhenExecuted(this ActionBase action,[CallerMemberName]string caller="") 
            => action.WhenExecuted(e => e.Observe(),caller);
        
        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecuted(this SingleChoiceAction action) 
            => action.ProcessEvent<SingleChoiceActionExecuteEventArgs>(nameof(SingleChoiceAction.Executed)).TakeUntilDisposed(action);
        
        
        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecuted(this ParametrizedAction action) 
            => action.ProcessEvent<ParametrizedActionExecuteEventArgs>(nameof(ParametrizedAction.Executed)).TakeUntilDisposed(action);
        
        

        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecuted(this PopupWindowShowAction action) 
            => action.ProcessEvent<PopupWindowShowActionExecuteEventArgs>(nameof(PopupWindowShowAction.Executed)).TakeUntilDisposed(action);
        
        

        public static IObservable<ItemsChangedEventArgs> WhenItemsChanged(this SingleChoiceAction action,ChoiceActionItemChangesType? changesType=null) 
            => action.ProcessEvent<ItemsChangedEventArgs>(nameof(SingleChoiceAction.ItemsChanged))
                .Where(e =>changesType==null||e.ChangedItemsInfo.Any(pair => pair.Value==changesType) ).TakeUntil(action.WhenDisposed());
        [Obsolete]
        public static IObservable<TAction> WhenExecuteConcat<TAction>(this TAction action) where TAction : ActionBase 
            => action.WhenDisabled().Where(a => a.Enabled.Contains(nameof(WhenConcatExecution)))
                .SelectMany(a => a.WhenEnabled().Where(ab1 => ab1.Enabled.Contains(nameof(WhenConcatExecution))).To(a));

        private static readonly ISubject<ActionBase> ExecuteFinishedSubject = Subject.Synchronize(new Subject<ActionBase>());
        public static IObservable<TAction> WhenExecuteFinished<TAction>(this TAction action,bool customEmit=false) where TAction : ActionBase
            => customEmit || (action.Data.ContainsKey(nameof(ExecutionFinished)) && (bool)action.Data[nameof(ExecutionFinished)])
                ? ExecuteFinishedSubject.Where(a => a == action).Cast<TAction>() : action.When<EventArgs,TAction>(nameof(ActionBase.ExecuteCompleted),_ => action.Observe());

        public static void CustomizeExecutionFinished<TAction>(this TAction action, bool enable=true) where TAction : ActionBase
            => action.Data[nameof(ExecutionFinished)] = enable;
        
        public static void ExecutionFinished<TAction>(this TAction action) where TAction:ActionBase 
            => ExecuteFinishedSubject.OnNext(action);

        public static IObservable<ActionBaseEventArgs> WhenExecuteCompleted(this IObservable<ActionBase> source) 
            => source.SelectMany(a => a.When<ActionBaseEventArgs,ActionBaseEventArgs>(nameof(ActionBase.ExecuteCompleted),e => e.Observe()));
        
        public static IObservable<TAction> WhenExecuteFinished<TAction>(this IObservable<TAction> source,bool customEmit=false) where TAction : ActionBase 
            => source.SelectMany(a=>a.WhenExecuteFinished(customEmit));
        
        public static IObservable<ActionBaseEventArgs> WhenExecuted<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.SelectMany(a=>a.WhenExecuted());

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecuted(this IObservable<SimpleAction> source) 
            => source.SelectMany(a=>a.WhenExecuted()).Cast<SimpleActionExecuteEventArgs>();
        
        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecuted(this IObservable<SingleChoiceAction> source) 
            => source.SelectMany(a=>a.WhenExecuted()).Cast<SingleChoiceActionExecuteEventArgs>();
        
        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecuted(this IObservable<PopupWindowShowAction> source) 
            => source.SelectMany(a=>a.WhenExecuted()).Cast<PopupWindowShowActionExecuteEventArgs>();
        
        public static IObservable<T> TakeUntilDisposed<T>(this IObservable<T> source, ActionBase component) 
            // => source.TakeWhileInclusive(_ => !component.IsDisposed);
            => source.TakeUntil(component.WhenDisposed());

        public static IObservable<(TAction action, BoolList boolList, BoolValueChangedEventArgs e)> ResultValueChanged<TAction>(
                this TAction source, Func<TAction, BoolList> boolListSelector,[CallerMemberName]string caller="") where TAction : ActionBase 
            => boolListSelector(source).Observe().ResultValueChanged(caller:caller).Select(tuple => (source, tuple.boolList, tuple.e));

        public static IObservable<SingleChoiceAction> WhenSelectedItemChanged(this IObservable<SingleChoiceAction> source) 
            =>source.SelectMany(action => action.WhenSelectedItemChanged());

        public static IObservable<SingleChoiceAction> WhenSelectedItemChanged(this SingleChoiceAction action) 
            => action.ProcessEvent(nameof(SingleChoiceAction.SelectedItemChanged)).To(action);

        public static TAction As<TAction>(this ActionBase action) where TAction:ActionBase 
            => ((TAction) action);

        public static IObservable<Unit> WhenDisposing<TAction>(this TAction simpleAction) where TAction : ActionBase 
            => Disposing(simpleAction.Observe());

        public static IObservable<TAction> WhenControllerActivated<TAction>(this IObservable<TAction> source,bool emitWhenActive=false) where TAction : ActionBase 
            => source.SelectMany(a =>a.Controller.WhenActivated(emitWhenActive).To(a) );
        
        public static IObservable<TAction> ActivateFor<TAction>(this IObservable<TAction> source,TemplateContext context) where TAction : ActionBase
            => source.WhenControllerActivated(action => action.Observe()
                .Do(simpleAction => action.Active[$"{nameof(ActivateFor)} {context}"]=simpleAction.Controller.Frame.Context==context).ToUnit());
        public static IObservable<TAction> ActivateFor<TAction>(this IObservable<TAction> source,Func<TAction,bool> condition) where TAction : ActionBase
            => source.WhenControllerActivated(action => action.Observe()
                .Do(simpleAction => simpleAction.Active[$"{nameof(ActivateFor)}"]=condition(simpleAction)).ToUnit());
        public static IObservable<TAction> EnableFor<TAction>(this IObservable<TAction> source,Func<TAction,bool> condition) where TAction : ActionBase
            => source.WhenControllerActivated(action => action.View().WhenSelectedObjectsChanged().To(action).StartWith(action)
                .Do(simpleAction => simpleAction.Enabled[$"{nameof(EnableFor)}"]=condition(simpleAction)).ToUnit());
        
        public static IObservable<TAction> WhenControllerActivated<TAction>(this IObservable<TAction> source,Func<TAction,IObservable<Unit>> mergeSelector,bool emitWhenActive=false) where TAction : ActionBase 
            => source.MergeIgnored(a =>a.Controller.WhenActivated(emitWhenActive).TakeUntil(a.Controller.WhenDeactivated())
                .SelectMany(_ => mergeSelector(a)).To(a) );
        
        public static IObservable<TAction> WhenControllerDeActivated<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.SelectMany(a =>a.Controller.WhenDeactivated().To(a) );

        public static IObservable<TAction> WhenActive<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.Where(a => a.Active);
        public static IObservable<TAction> WhereAvailable<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.Where(a => a.Available());

        public static IObservable<TAction> WhenInActive<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.Where(a => !a.Active);

        public static IObservable<TAction> WhenActive<TAction>(this TAction simpleAction) where TAction : ActionBase 
            => simpleAction.Observe().WhenActive();

		public static IObservable<TAction> WhenActivated<TAction>(this IObservable<TAction> source,string[] contexts=null,[CallerMemberName]string caller="") where TAction : ActionBase 
            => source.SelectMany(a => a.WhenActivated(contexts,caller));
		
		public static IObservable<TAction> WhenInActive<TAction>(this TAction simpleAction) where TAction : ActionBase 
            => simpleAction.Observe().WhenInActive();

		public static IObservable<TAction> WhenDeactivated<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.SelectMany(a => a.WhenDeactivated());

        public static IObservable<TAction> WhenActivated<TAction>(this TAction simpleAction,string[] contexts=null,[CallerMemberName]string caller="") where TAction : ActionBase 
            => simpleAction.ResultValueChanged(action => action.Active,caller:caller).SelectMany(t => (contexts ??[]).Concat(Controller.ControllerActiveKey.YieldItem()).Select(context => (t,context)))
		        .Where(t => t.t.action.Active.ResultValue&&t.t.action.Active.Contains(t.context)&& t.t.action.Active[t.context])
		        .Select(t => t.t.action);
        
        public static IObservable<TAction> WhenDeactivated<TAction>(this TAction simpleAction) where TAction : ActionBase 
            => simpleAction.ResultValueChanged(action => action.Active)
		        .Where(tuple => !tuple.action.Active.ResultValue)
		        .Select(t => t.action);
        
        public static IObservable<TAction> WhenDisabled<TAction>(this TAction simpleAction,params string[] contexts) where TAction : ActionBase 
            => simpleAction.ResultValueChanged(action => action.Enabled)
		        .Where(tuple => !tuple.action.Enabled.ResultValue)
                .SelectMany(t =>contexts.Any()? contexts.Where(context => t.action.Enabled.Contains(context)&&!t.action.Enabled[context]).To(t):t.YieldItem())
		        .Select(t => t.action);

        public static IObservable<TAction> WhenChanged<TAction>(this IObservable<TAction> source,ActionChangedType? actionChangedType = null)where TAction : ActionBase 
            => source.SelectMany(a => a.WhenChanged(actionChangedType));

        public static IObservable<TAction> WhenChanged<TAction>(this TAction action, ActionChangedType? actionChangedType = null) where TAction : ActionBase 
            => action.ProcessEvent<ActionChangedEventArgs,TAction>(nameof(ActionBase.Changed), e => e.Observe()
                .Where(eventArgs => actionChangedType == null || eventArgs.ChangedPropertyType == actionChangedType).To(action));

        public static IObservable<TAction> WhenEnable<TAction>(this IObservable<TAction> source)where TAction : ActionBase 
            => source.Where(a => a.Enabled);

        public static IObservable<TAction> WhenEnable<TAction>(this TAction simpleAction) where TAction : ActionBase 
            =>simpleAction.Observe().WhenEnable();

        public static IObservable<TAction> WhenEnabled<TAction>(this IObservable<TAction> source)where TAction : ActionBase 
            => source.SelectMany(a => a.WhenEnabled());

        public static IObservable<TAction> WhenEnabled<TAction>(this TAction simpleAction) where TAction : ActionBase 
            =>simpleAction.ResultValueChanged(action => action.Enabled).Where(tuple => tuple.action.Enabled.ResultValue)
                .Select(t => t.action);

        public static IObservable<Unit> Disposing<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source .SelectMany(item => item.ProcessEvent(nameof(ActionBase.Disposing)).ToUnit());

        public static IObservable<ParametrizedAction> WhenValueChanged(this ParametrizedAction action)
            => action.ProcessEvent(nameof(ParametrizedAction.ValueChanged)).TakeUntilDisposed(action);

        public static IObservable<Unit> Trigger<TAction>(this IObservable<TAction> source) where TAction : ActionBase
            => source.SelectMany(action => action.Trigger());
        
        public static IObservable<TAction> TriggerWhenActivated<TAction>(this IObservable<TAction> source) where TAction:ActionBase 
            => source.MergeIgnored(@base => @base.Observe().WhenControllerActivated(action => action.Trigger()));

        public static IObservable<TAction> ActivateInLookupListView<TAction>(this IObservable<TAction> source) where TAction:ActionBase
            => source.WhenControllerActivated().Do(action => action.Active[nameof(ActivateInLookupListView)]=action.Frame().Template is ILookupPopupFrameTemplate);
        
        public static IObservable<TAction> ActivateInUserDetails<TAction>(this IObservable<TAction> registerAction) where TAction:ActionBase 
	        => registerAction.WhenControllerActivated()
                .Do(action => {
                    bool active=false;
                    if (!string.IsNullOrEmpty(SecuritySystem.CurrentUserName)) {
                        var view = action.View();
                        active =view is DetailView&& view.CurrentObject != null && view.ObjectSpace.GetKeyValue(view.CurrentObject)?.ToString() == SecuritySystem.CurrentUserId.ToString();    
                    }
                    action.Active[nameof(ActivateInUserDetails)] = active;
		        })
                .WhenNotDefault(a => a.Active[nameof(ActivateInUserDetails)])
		        .TraceRX(action => $"{action.Id}, {SecuritySystem.CurrentUserName}");

        public static IObservable<(TAction action, CustomizeControlEventArgs e)> WhenCustomizeControl<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.SelectMany(a => a.WhenCustomizeControl().InversePair(a));
        public static IObservable<TAction> WhenCustomizeControl<TAction>(this IObservable<TAction> source,Func<(TAction action,CustomizeControlEventArgs e),IObservable<Unit>> selector) where TAction : ActionBase 
            => source.MergeIgnored(a => a.WhenCustomizeControl().InversePair(a).SelectMany(selector));

        public static IObservable<CustomizeControlEventArgs> WhenCustomizeControl<TAction>(this TAction action) where TAction:ActionBase 
            => action.ProcessEvent<CustomizeControlEventArgs>(nameof(ActionBase.CustomizeControl));
        
        public static IObservable<T2> WhenUpdating<T2>(this ActionBase action,Func<UpdateActionEventArgs,IObservable<T2>> selector) 
            => action.Controller.WhenActivated(true)
                .SelectManyUntilDeactivated(controller => controller.Frame.GetController<ActionsCriteriaViewController>()
                    .ProcessEvent<UpdateActionEventArgs>(nameof(ActionsCriteriaViewController.ActionUpdating)).Where(e => e.Active&&e.NeedUpdateEnabled)
                    .SelectMany(selector));

        public static IObservable<DialogController> CreateDialogController(this ActionBaseEventArgs e,ObjectView objectView,string caption=null,bool refreshViewAfterObjectSpaceCommit=true,bool closeOnCancel=true,TargetWindow targetWindow=TargetWindow.NewModalWindow){
            var application = e.Application();
            var parameters = e.ShowViewParameters;
            parameters.TargetWindow=targetWindow;
            parameters.CreateAllControllers = true;
            var dialogController = application.CreateController<DialogController>();
            if (caption != null) {
                dialogController.AcceptAction.Caption = caption;
            }
            dialogController.CanCloseWindow = !closeOnCancel;
            if (closeOnCancel) {
                dialogController.CancelAction.ActionMeaning = ActionMeaning.Accept;    
            }
            parameters.Controllers.Add(dialogController);
            parameters.CreatedView=objectView;
            return dialogController.WhenFrame()
                .DoWhen(_ => refreshViewAfterObjectSpaceCommit,frame => {
                    var modificationsController = frame.GetController<ModificationsController>();
                    modificationsController?.SetPropertyValue("RefreshViewAfterObjectSpaceCommit", false);
                })
                .IgnoreElements().To<DialogController>().StartWith(dialogController)
                .If(_ => closeOnCancel,controller => controller.CancelAction.WhenExecuted(_ => {
                        e.Action.View().ObjectSpace.Rollback(askConfirmation:false);
                        e.ShowViewParameters.CreatedView.Close();
                        return Observable.Empty<DialogController>();
                    })
                    .IgnoreElements().To<DialogController>().StartWith(dialogController));
        }

        
        public static IObservable<T> Trigger<T>(this SingleChoiceAction action, IObservable<T> afterExecuted,params object[] selection)
            => action.Trigger(afterExecuted,() => action.SelectedItem,selection);
        
        public static IObservable<Unit> Trigger(this SingleChoiceAction action, params object[] selection)
            => action.Trigger(Observable.Empty<Unit>(),() => action.SelectedItem??action.Items.FirstOrDefault(),selection);
        public static IObservable<Unit> Trigger(this SingleChoiceAction action, Func<ChoiceActionItem> selectedItem)
            => action.Trigger(Observable.Empty<Unit>(),selectedItem);
        
        public static IObservable<T> Trigger<T>(this SingleChoiceAction action, IObservable<T> afterExecuted,Func<ChoiceActionItem> selectedItem,params object[] selection)
            => afterExecuted.Trigger(() => action.DoExecute(selectedItem(), selection));

        public static IObservable<CustomizeTemplateEventArgs> WhenCustomizeTemplate(this PopupWindowShowAction action) 
            => action.ProcessEvent<CustomizeTemplateEventArgs>(nameof(action.CustomizeTemplate)).TakeUntilDisposed(action);

        public static IObservable<T> Trigger<T>(this PopupWindowShowAction action, IObservable<T> afterExecuted) 
            => action.ShowPopupWindow().ToController<DialogController>().DelayOnContext()
                .SelectMany(controller => controller.AcceptAction.Trigger(afterExecuted));

        public static IObservable<Frame> LinkObject(this PopupWindowShowAction action) 
            => action.Application.WhenFrame().When(TemplateContext.LookupWindowContextName).Take(1)
                .If(frame => ((ILookupPopupFrameTemplate)frame.Template).IsSearchEnabled,frame => frame.GetController<FilterController>().FullTextFilterAction
                    .Trigger(frame.View.WhenObjects().Take(1).To(frame)),frame => frame.View.WhenObjects().Take(1).To(frame) ).IgnoreElements()
                .Merge(action.Trigger(action.WhenExecuteCompleted().To(action.Frame())));

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

        public static IObservable<ParametrizedAction> WhenValueChangedTrigger(this IObservable<ParametrizedAction> source)
            => source.WhenValueChangedApplyValue(action => action.Trigger());
        public static IObservable<ParametrizedAction> WhenValueChangedApplyValue(this IObservable<ParametrizedAction> source,Func<ParametrizedAction,IObservable<Unit>> selector=null)
            => source.WhenCustomizeControl(t => t.e.Control.Observe()
                .SelectMany(spinEdit => spinEdit.ProcessEvent("ValueChanged")
                    .Select(_ => spinEdit.GetPropertyValue("EditValue")).WhenNotDefault()
                    .Do(value =>t.action.Value=value )
                    .WaitUntilInactive(1.Seconds()).ObserveOnContext().To(t.action)
                    .SelectMany(action => selector?.Invoke(action)??Observable.Empty<Unit>())));
    }
}