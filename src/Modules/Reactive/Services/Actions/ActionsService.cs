using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Utils;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.Reactive.Services.Actions{
    
    public static partial class ActionsService{
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

        public static IObservable<T> WhenExecute<T>(this IObservable<SimpleAction> source,Func<SimpleActionExecuteEventArgs, IObservable<T>> retriedExecution) 
            => source.SelectMany(action => action.WhenExecute(retriedExecution).TakeUntilDeactivated(action.Controller));
        public static IObservable<T> WhenExecuted<T>(this IObservable<SimpleAction> source,Func<SimpleActionExecuteEventArgs, IObservable<T>> retriedExecution) 
            => source.SelectMany(action => action.WhenExecuted(retriedExecution).TakeUntilDeactivated(action.Controller));
        public static IObservable<Unit> WhenExecuted(this IObservable<SimpleAction> source,Action<SimpleActionExecuteEventArgs> retriedExecution) 
            => source.SelectMany(action => action.WhenExecuted(args => {
                retriedExecution(args);
                return Unit.Default.ReturnObservable();
            }).TakeUntilDeactivated(action.Controller));
        
        public static IObservable<T> WhenExecuted<T>(this IObservable<SingleChoiceAction> source,Func<SingleChoiceActionExecuteEventArgs, IObservable<T>> retriedExecution) 
            => source.SelectMany(action => action.WhenExecuted(retriedExecution).TakeUntilDeactivated(action.Controller));

        public static IObservable<T> WhenExecute<T>(this SimpleAction simpleAction,Func<SimpleActionExecuteEventArgs, IObservable<T>> retriedExecution) 
            => simpleAction.WhenExecute().SelectMany(retriedExecution).Retry(() => simpleAction.Application).TakeUntilDeactivated(simpleAction.Controller);
        public static IObservable<Unit> WhenExecute(this SimpleAction simpleAction,Action<SimpleActionExecuteEventArgs> retriedExecution) 
            => simpleAction.WhenExecute(e => {
                retriedExecution(e);
                return Observable.Empty<Unit>();
            });
        
        public static IObservable<Unit> WhenExecuted(this ParametrizedAction simpleAction,Action<ParametrizedActionExecuteEventArgs> retriedExecution) 
            => simpleAction.WhenExecute(e => {
                retriedExecution(e);
                return Observable.Empty<Unit>();
            });
        
        public static IObservable<T> WhenExecuted<T>(this SimpleAction simpleAction,Func<SimpleActionExecuteEventArgs, IObservable<T>> retriedExecution) 
            => simpleAction.WhenExecuted().SelectMany(retriedExecution).Retry(() => simpleAction.Application).TakeUntilDeactivated(simpleAction.Controller);
        
        public static IObservable<T> WhenExecuted<T>(this ParametrizedAction simpleAction,Func<ParametrizedActionExecuteEventArgs, IObservable<T>> retriedExecution) 
            => simpleAction.WhenExecuted().SelectMany(retriedExecution).Retry(() => simpleAction.Application).TakeUntilDeactivated(simpleAction.Controller);
        
        public static IObservable<Unit> WhenExecuted(this SimpleAction simpleAction,Action<SimpleActionExecuteEventArgs> retriedExecution) 
            => simpleAction.WhenExecuted(e => {
                retriedExecution(e);
                return Observable.Empty<Unit>();
            });

        public static IObservable<Unit> WhenConcatExecution(this SimpleAction action,Action<SimpleActionExecuteEventArgs> retriedExecution) 
            => action.AsSimpleAction().WhenExecuted(e => {
                e.Action.Enabled[nameof(WhenConcatExecution)] = false;
                try{
                    retriedExecution?.Invoke(e);
                }
                finally{
                    e.Action.Enabled[nameof(WhenConcatExecution)] = true;
                }
            });

        public static IObservable<T> WhenConcatRetriedExecution<T>(this SimpleAction simpleAction,Func<SimpleActionExecuteEventArgs, IObservable<T>> retriedExecution)
            => simpleAction.WhenExecuted(e => {
                e.Action.Enabled[nameof(WhenConcatExecution)] = false;
                return retriedExecution.Invoke(e).ObserveOnContext().Finally(() => e.Action.Enabled[nameof(WhenConcatExecution)] = true);
            });

        public static IObservable<T> WhenConcatExecution<T>(this IObservable<SimpleAction> source, Func<SimpleActionExecuteEventArgs, IObservable<T>> sourceSelector)
            => source.SelectMany(action => action.WhenConcatExecution(sourceSelector));
        
        public static IObservable<T> WhenConcatExecution<T>(this SimpleAction simpleAction,Func<SimpleActionExecuteEventArgs,IObservable<T>> sourceSelector)
            => simpleAction.WhenExecuted(e => {
                    simpleAction.Enabled[nameof(WhenConcatExecution)] = false;
                    return sourceSelector(e).ObserveOnContext().Finally(() => simpleAction.Enabled[nameof(WhenConcatExecution)] = true);
                });
        
        public static IObservable<Unit> WhenConcatExecution(this ParametrizedAction action,Action<ParametrizedActionExecuteEventArgs> retriedExecution) 
            => action.AsParametrizedAction().WhenExecuted(e => {
                e.Action.Enabled[nameof(WhenConcatExecution)] = false;
                try{
                    retriedExecution?.Invoke(e);
                }
                finally{
                    e.Action.Enabled[nameof(WhenConcatExecution)] = true;
                }
            });

        public static IObservable<T> WhenConcatRetriedExecution<T>(this ParametrizedAction simpleAction,Func<ParametrizedActionExecuteEventArgs, IObservable<T>> retriedExecution)
            => simpleAction.WhenExecuted(e => {
                e.Action.Enabled[nameof(WhenConcatExecution)] = false;
                return retriedExecution.Invoke(e).ObserveOnContext().Finally(() => e.Action.Enabled[nameof(WhenConcatExecution)] = true);
            });
        public static IObservable<T> WhenConcatExecution<T>(this ParametrizedAction simpleAction,Func<ParametrizedActionExecuteEventArgs,IObservable<T>> sourceSelector)
            => simpleAction.WhenConcatExecution<T,ParametrizedActionExecuteEventArgs>( sourceSelector);
        
        public static IObservable<T> WhenConcatExecution<T>(this SingleChoiceAction simpleAction,Func<SingleChoiceActionExecuteEventArgs,IObservable<T>> sourceSelector)
            => simpleAction.WhenConcatExecution<T,SingleChoiceActionExecuteEventArgs>( sourceSelector);
        
        private static IObservable<T> WhenConcatExecution<T,TArgs>(this ActionBase action, Func<TArgs, IObservable<T>> sourceSelector)  where TArgs:ActionBaseEventArgs 
            => action.WhenExecuted().Do(_ => action.Enabled[nameof(WhenConcatExecution)] = false).Cast<TArgs>()
                .SelectManySequential(e => sourceSelector(e).ObserveOnContext().Finally(() => action.Enabled[nameof(WhenConcatExecution)] = true));

        public static IObservable<T> WhenExecuted<T>(this SingleChoiceAction simpleAction,Func<SingleChoiceActionExecuteEventArgs, IObservable<T>> retriedExecution) 
            => simpleAction.WhenExecuted().SelectMany(retriedExecution).Retry(() => simpleAction.Application).TakeUntilDeactivated(simpleAction.Controller);
        
        public static IObservable<T> WhenExecute<T>(this IObservable<SingleChoiceAction> source,Func<SingleChoiceActionExecuteEventArgs, IObservable<T>> retriedExecution) 
            => source.SelectMany(action => action.WhenExecute(retriedExecution).TakeUntilDeactivated(action.Controller));

        public static IObservable<T> WhenExecute<T>(this SingleChoiceAction singleChoiceAction,Func<SingleChoiceActionExecuteEventArgs, IObservable<T>> retriedExecution) 
            => singleChoiceAction.WhenExecute().SelectMany(retriedExecution).Retry(() => singleChoiceAction.Application).TakeUntilDeactivated(singleChoiceAction.Controller);
        
        public static IObservable<T> WhenExecute<T>(this IObservable<ParametrizedAction> source,Func<ParametrizedActionExecuteEventArgs, IObservable<T>> retriedExecution) 
            => source.SelectMany(action => action.WhenExecute(retriedExecution).TakeUntilDeactivated(action.Controller));

        public static IObservable<T> WhenExecute<T>(this ParametrizedAction action,Func<ParametrizedActionExecuteEventArgs, IObservable<T>> retriedExecution) 
            => action.WhenExecute().SelectMany(retriedExecution).Retry(() => action.Application).TakeUntilDeactivated(action.Controller);
        
        public static IObservable<T> WhenExecute<T>(this IObservable<PopupWindowShowAction> source,Func<PopupWindowShowActionExecuteEventArgs, IObservable<T>> retriedExecution) 
            => source.SelectMany(action => action.WhenExecute(retriedExecution).TakeUntilDeactivated(action.Controller));

        public static IObservable<T> WhenExecute<T>(this PopupWindowShowAction action,Func<PopupWindowShowActionExecuteEventArgs, IObservable<T>> retriedExecution) 
            => action.WhenExecute().SelectMany(retriedExecution).Retry(() => action.Application).TakeUntilDeactivated(action.Controller);

        public static IObservable<T> CommitChanges<T>(this IObservable<T> source) where T : ActionBaseEventArgs
            => source.Do(args => {
                var view = args.Action.View();
                view?.AsObjectView()?.ObjectSpace.CommitChanges();
            });

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecute(this IObservable<SimpleAction> source) 
            => source.SelectMany(action => action.WhenExecute());
        
        public static IObservable<(SimpleAction action, CancelEventArgs e)> WhenExecuting(this IObservable<SimpleAction> source) 
            => source.SelectMany(action => action.WhenExecuting());
        
        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecute(this IObservable<ParametrizedAction> source) 
            => source.SelectMany(action => action.WhenExecute());
        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecuted(this IObservable<ParametrizedAction> source) 
            => source.SelectMany(action => action.WhenExecuted());
        public static IObservable<Unit> WhenConcatExecution(this IObservable<ParametrizedAction> source,Action<ParametrizedActionExecuteEventArgs> retriedExecution) 
            => source.SelectMany(action => action.WhenConcatExecution(retriedExecution));
        // public static IObservable<Unit> WhenConcatExecution(this IObservable<SingleChoiceAction> source,Action<SingleChoiceActionExecuteEventArgs> retriedExecution) 
        //     => source.SelectMany(action => action.WhenConcatExecution(retriedExecution));
        
        public static IObservable<Unit> WhenConcatExecution(this IObservable<SimpleAction> source,Action<SimpleActionExecuteEventArgs> retriedExecution) 
            => source.SelectMany(action => action.WhenConcatExecution(retriedExecution));
        
        public static IObservable<T> WhenConcatExecution<T>(this IObservable<ParametrizedAction> source,Func<ParametrizedActionExecuteEventArgs,IObservable<T>> selector) 
            => source.SelectMany(action => action.WhenConcatExecution(selector));
        
        public static IObservable<T> WhenConcatExecution<T>(this IObservable<SingleChoiceAction> source,Func<SingleChoiceActionExecuteEventArgs,IObservable<T>> selector) 
            => source.SelectMany(action => action.WhenConcatExecution(selector));

        public static IObservable<T> WhenExecuted<T>(this IObservable<ParametrizedAction> source,Func<ParametrizedActionExecuteEventArgs, IObservable<T>> retriedExecution) 
            => source.SelectMany(action => action.WhenExecuted(retriedExecution));
        
        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecute(this IObservable<PopupWindowShowAction> source) 
            => source.SelectMany(action => action.WhenExecute());

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecute(this SimpleAction simpleAction) 
            => Observable.FromEventPattern<SimpleActionExecuteEventHandler, SimpleActionExecuteEventArgs>(
			        h => simpleAction.Execute += h, h => simpleAction.Execute -= h, ImmediateScheduler.Instance)
		        .Select(pattern => pattern.EventArgs);
        
        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecute(this PopupWindowShowAction action) 
            => Observable.FromEventPattern<PopupWindowShowActionExecuteEventHandler, PopupWindowShowActionExecuteEventArgs>(
			        h => action.Execute += h, h => action.Execute -= h, ImmediateScheduler.Instance)
		        .Select(pattern => pattern.EventArgs);
        
        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecute(this ParametrizedAction action) 
            => Observable.FromEventPattern<ParametrizedActionExecuteEventHandler, ParametrizedActionExecuteEventArgs>(
			        h => action.Execute += h, h => action.Execute -= h, ImmediateScheduler.Instance)
		        .Select(pattern => pattern.EventArgs);

        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecute(this SingleChoiceAction singleChoiceAction) 
            => Observable.FromEventPattern<SingleChoiceActionExecuteEventHandler, SingleChoiceActionExecuteEventArgs>(
			        h => singleChoiceAction.Execute += h, h => singleChoiceAction.Execute -= h, ImmediateScheduler.Instance)
		        .Select(pattern => pattern.EventArgs);

        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecute(this IObservable<SingleChoiceAction> source) 
            => source.SelectMany(action => action.WhenExecute());

        public static IObservable<TFrame> WhenView<TFrame>(this IObservable<TFrame> source, Type objectType) where TFrame : Frame 
            => source.SelectMany(frame => frame.View.ReturnObservable().When(objectType).Select(_ => frame));

        public static IObservable<TAction> When<TAction>(this IObservable<TAction> source, Type objectType) where TAction : ActionBase 
            => source.Where(_ => objectType.IsAssignableFrom(_.Controller.Frame.View.ObjectTypeInfo.Type));

        public static IObservable<IObjectSpace> ToObjectSpace<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.Select(_ => _.Controller.Frame.View.ObjectSpace);

        public static IObservable<(TAction action, CancelEventArgs e)> WhenExecuting<TAction>(this TAction action) where TAction : ActionBase 
            => Observable.FromEventPattern<CancelEventHandler, CancelEventArgs>(h => action.Executing += h,
		        h => action.Executing -= h, ImmediateScheduler.Instance).TransformPattern<CancelEventArgs, TAction>();

        public static  IObservable<(TAction action, Type objectType, View view, Frame frame, IObjectSpace objectSpace, ShowViewParameters showViewParameters)> ToParameter<TAction>(
                this IObservable<(TAction action, ActionBaseEventArgs e)> source) where TAction : ActionBase => source.Select(_ => {
		        var frame = _.action.Controller.Frame;
		        return (_.action, frame.View.ObjectTypeInfo.Type, frame.View, frame, frame.View.ObjectSpace, _.e.ShowViewParameters);
	        });

        public static IObservable<TAction> ToAction<TAction>(this IObservable<(TAction action, ActionBaseEventArgs e)> source) where TAction : ActionBase 
            => source.Select(_ => _.action);

        public static IObservable<CustomizePopupWindowParamsEventArgs> WhenCustomizePopupWindowParams(this PopupWindowShowAction action) 
            => Observable.FromEventPattern<CustomizePopupWindowParamsEventHandler, CustomizePopupWindowParamsEventArgs>(
			        h => action.CustomizePopupWindowParams += h, h => action.CustomizePopupWindowParams -= h, ImmediateScheduler.Instance)
		        .Select(pattern => pattern.EventArgs);

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecuted(this SimpleAction action) 
            => Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
		        h => action.Executed += h, h => action.Executed -= h, ImmediateScheduler.Instance).Select(_ =>(SimpleActionExecuteEventArgs) _.EventArgs);

        public static IObservable<SingleChoiceAction> AddItems(this IObservable<SingleChoiceAction> source,Func<SingleChoiceAction,IObservable<Unit>> addItems)
            => source.WhenControllerActivated().ConcatIgnored(addItems).WhenActive()
                .Merge(source.WhenControllerDeActivated().Do(action => action.Items.Clear()).IgnoreElements())
        ;

        public static IObservable<TArgs> CreateDetailView<TArgs>(this IObservable<TArgs> source, Type objectType, TargetWindow? targetWindow =null) where TArgs:ActionBaseEventArgs
	        => source.Do(e => {
		        var parameters = e.ShowViewParameters;
		        parameters.CreatedView = e.Action.Application.NewDetailView(objectType);
                parameters.CreatedView.CurrentObject = parameters.CreatedView.ObjectSpace.CreateObject(objectType);
		        if (targetWindow.HasValue) {
			        parameters.TargetWindow = targetWindow.Value;
		        }
	        });

        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecuted(this SingleChoiceAction action) 
            => Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
		        h => action.Executed += h, h => action.Executed -= h, ImmediateScheduler.Instance).Select(_ => (SingleChoiceActionExecuteEventArgs)_.EventArgs);

        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecuted(this ParametrizedAction action) 
            => Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
		        h => action.Executed += h, h => action.Executed -= h, ImmediateScheduler.Instance).Select(_ => (ParametrizedActionExecuteEventArgs)_.EventArgs);

        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecuted(this PopupWindowShowAction action) 
            => Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
		        h => action.Executed += h, h => action.Executed -= h, ImmediateScheduler.Instance).Select(_ => (PopupWindowShowActionExecuteEventArgs)_.EventArgs);

        public static IObservable<ActionBaseEventArgs> WhenExecuted<TAction>(this TAction action) where TAction : ActionBase 
            => Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
			        h => action.Executed += h, h => action.Executed -= h, ImmediateScheduler.Instance)
		        .Select(pattern => pattern.EventArgs);

        public static IObservable<ActionBaseEventArgs> WhenExecuteCompleted<TAction>(this TAction action) where TAction : ActionBase 
            => Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
			        h => action.ExecuteCompleted += h, h => action.ExecuteCompleted -= h, ImmediateScheduler.Instance)
		        .Select(pattern => pattern.EventArgs);
        
        public static IObservable<TAction> WhenExecuteConcat<TAction>(this TAction action) where TAction : ActionBase 
            => action.WhenDisabled().Where(a => a.Enabled.Contains(nameof(WhenConcatExecution)))
                .SelectMany(a => a.WhenEnabled().Where(ab1 => ab1.Enabled.Contains(nameof(WhenConcatExecution))).To(a));

        private static readonly ISubject<ActionBase> ExecuteFinishedSubject = Subject.Synchronize(new Subject<ActionBase>());
        public static IObservable<TAction> WhenExecuteFinished<TAction>(this TAction action,bool customEmit=false) where TAction : ActionBase
            => customEmit || (action.Data.ContainsKey(nameof(ExecutionFinished)) && (bool)action.Data[nameof(ExecutionFinished)])
                ? ExecuteFinishedSubject.Where(a => a == action).Cast<TAction>() : action.WhenExecuteCompleted().To(action);

        public static void CustomizeExecutionFinished<TAction>(this TAction action, bool enable=true)
            where TAction : ActionBase
            => action.Data[nameof(ExecutionFinished)] = enable;
        
        public static void ExecutionFinished<TAction>(this TAction action) where TAction:ActionBase 
            => ExecuteFinishedSubject.OnNext(action);

        public static IObservable<TAction> WhenExecuteFinished<TAction>(this IObservable<TAction> source,bool customEmit=false) where TAction : ActionBase 
            => source.SelectMany(a=>a.WhenExecuteFinished(customEmit));
        public static IObservable<ActionBaseEventArgs> WhenExecuteCompleted<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.SelectMany(a=>a.WhenExecuteCompleted());
        public static IObservable<TAction> WhenExecuteConcat<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.SelectMany(a=>a.WhenExecuteConcat());
        
        public static IObservable<ActionBaseEventArgs> WhenExecuted<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.SelectMany(a=>a.WhenExecuted());

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecuted(this IObservable<SimpleAction> source) 
            => source.SelectMany(a=>a.WhenExecuted()).Cast<SimpleActionExecuteEventArgs>();
        
        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecuted(this IObservable<SingleChoiceAction> source) 
            => source.SelectMany(a=>a.WhenExecuted()).Cast<SingleChoiceActionExecuteEventArgs>();
        
        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecuted(this IObservable<PopupWindowShowAction> source) 
            => source.SelectMany(a=>a.WhenExecuted()).Cast<PopupWindowShowActionExecuteEventArgs>();

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecuteCompleted(this SimpleAction action) 
            => Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
			        h => action.ExecuteCompleted += h, h => action.ExecuteCompleted -= h, ImmediateScheduler.Instance)
		        .Select(pattern => (SimpleActionExecuteEventArgs) pattern.EventArgs);

        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecuteCompleted(this SingleChoiceAction action) 
            => Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
			        h => action.ExecuteCompleted += h, h => action.ExecuteCompleted -= h, ImmediateScheduler.Instance)
		        .Select(pattern => (SingleChoiceActionExecuteEventArgs)pattern.EventArgs);

        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecuteCompleted(this ParametrizedAction action) 
            => Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
			        h => action.ExecuteCompleted += h, h => action.ExecuteCompleted -= h, ImmediateScheduler.Instance)
		        .Select(pattern => (ParametrizedActionExecuteEventArgs)pattern.EventArgs);

        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecuteCompleted(this PopupWindowShowAction action) 
            => Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
			        h => action.ExecuteCompleted += h, h => action.ExecuteCompleted -= h, ImmediateScheduler.Instance)
		        .Select(pattern => (PopupWindowShowActionExecuteEventArgs)pattern.EventArgs);

        public static IObservable<(TAction action, BoolList boolList, BoolValueChangedEventArgs e)> ResultValueChanged<TAction>(
                this TAction source, Func<TAction, BoolList> boolListSelector) where TAction : ActionBase 
            => boolListSelector(source).ReturnObservable().ResultValueChanged().Select(tuple => (source, tuple.boolList, tuple.e));

        public static IObservable<SingleChoiceAction> WhenSelectedItemChanged(this IObservable<SingleChoiceAction> source) 
            =>source.SelectMany(action => action.WhenSelectedItemChanged());

        public static IObservable<SingleChoiceAction> WhenSelectedItemChanged(this SingleChoiceAction action) 
            => Observable.FromEventPattern<EventHandler,EventArgs>(h => action.SelectedItemChanged+=h,h => action.SelectedItemChanged-=h,ImmediateScheduler.Instance)
		        .Select(_ => _.Sender).Cast<SingleChoiceAction>();

        public static TAction As<TAction>(this ActionBase action) where TAction:ActionBase 
            => ((TAction) action);

        public static IObservable<Unit> WhenDisposing<TAction>(this TAction simpleAction) where TAction : ActionBase 
            => Disposing(simpleAction.ReturnObservable());

        public static IObservable<TAction> WhenControllerActivated<TAction>(this IObservable<TAction> source,bool emitWhenActive=false) where TAction : ActionBase 
            => source.SelectMany(a =>a.Controller.WhenActivated(emitWhenActive).To(a) );
        
        public static IObservable<TAction> WhenControllerDeActivated<TAction>(this IObservable<TAction> source,bool emitWhenActive=false) where TAction : ActionBase 
            => source.SelectMany(a =>a.Controller.WhenDeactivated().To(a) );

        public static IObservable<TAction> WhenActive<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.Where(a => a.Active);
        public static IObservable<TAction> WhenAvailable<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.Where(a => a.Available());

        public static IObservable<TAction> WhenInActive<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.Where(a => !a.Active);

        public static IObservable<TAction> WhenActive<TAction>(this TAction simpleAction) where TAction : ActionBase 
            => simpleAction.ReturnObservable().WhenActive();

		public static IObservable<TAction> WhenActivated<TAction>(this IObservable<TAction> source,params string[] contexts) where TAction : ActionBase 
            => source.SelectMany(a => a.WhenActivated());
		
		public static IObservable<TAction> WhenInActive<TAction>(this TAction simpleAction) where TAction : ActionBase 
            => simpleAction.ReturnObservable().WhenInActive();

		public static IObservable<TAction> WhenDeactivated<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.SelectMany(a => a.WhenDeactivated());

        public static IObservable<TAction> WhenActivated<TAction>(this TAction simpleAction,params string[] contexts) where TAction : ActionBase 
            => simpleAction.ResultValueChanged(action => action.Active).SelectMany(t => contexts.Concat(Controller.ControllerActiveKey.YieldItem()).Select(context => (t,context)))
		        .Where(t => t.t.action.Active.ResultValue&&t.t.action.Active.Contains(t.context)&& t.t.action.Active[t.context])
		        .Select(t => t.t.action);
        
        public static IObservable<TAction> WhenDeactivated<TAction>(this TAction simpleAction) where TAction : ActionBase 
            => simpleAction.ResultValueChanged(action => action.Active)
		        .Where(tuple => !tuple.action.Active.ResultValue)
		        .Select(t => t.action);
        
        public static IObservable<TAction> WhenDisabled<TAction>(this TAction simpleAction) where TAction : ActionBase 
            => simpleAction.ResultValueChanged(action => action.Enabled)
		        .Where(tuple => !tuple.action.Enabled.ResultValue)
		        .Select(t => t.action);

        public static IObservable<TAction> WhenChanged<TAction>(this IObservable<TAction> source,ActionChangedType? actionChangedType = null)where TAction : ActionBase 
            => source.SelectMany(a => a.WhenChanged(actionChangedType));

        public static IObservable<TAction> WhenChanged<TAction>(this TAction action, ActionChangedType? actionChangedType = null) where TAction : ActionBase 
            => Observable.FromEventPattern<EventHandler<ActionChangedEventArgs>, ActionChangedEventArgs>(
			        h => action.Changed += h, h => action.Changed -= h)
		        .Where(pattern =>actionChangedType==null|| pattern.EventArgs.ChangedPropertyType == actionChangedType)
		        .Select(pattern => pattern.Sender).Cast<TAction>();

        public static IObservable<TAction> WhenEnable<TAction>(this IObservable<TAction> source)where TAction : ActionBase 
            => source.Where(a => a.Enabled);

        public static IObservable<TAction> WhenEnable<TAction>(this TAction simpleAction) where TAction : ActionBase 
            =>simpleAction.ReturnObservable().WhenEnable();

        public static IObservable<TAction> WhenEnabled<TAction>(this IObservable<TAction> source)where TAction : ActionBase 
            => source.SelectMany(a => a.WhenEnabled());

        public static IObservable<TAction> WhenEnabled<TAction>(this TAction simpleAction) where TAction : ActionBase 
            =>simpleAction
	        .ResultValueChanged(action => action.Enabled)
	        .Where(tuple => tuple.action.Enabled.ResultValue)
	        .Select(_ => _.action);

        public static IObservable<Unit> Disposing<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source .SelectMany(item => Observable.FromEventPattern<EventHandler, EventArgs>(h => item.Disposing += h,
			        h => item.Disposing -= h, ImmediateScheduler.Instance)
		        .Select(pattern => pattern).ToUnit());

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

        public static IObservable<(TAction sender, CustomizeControlEventArgs e)> WhenCustomizeControl<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.SelectMany(a => a.WhenCustomizeControl());

        public static IObservable<(TAction sender, CustomizeControlEventArgs e)> WhenCustomizeControl<TAction>(this TAction action) where TAction:ActionBase 
            => Observable.FromEventPattern<EventHandler<CustomizeControlEventArgs>, CustomizeControlEventArgs>(
			        h => action.CustomizeControl += h, h => action.CustomizeControl -= h, ImmediateScheduler.Instance)
		        .TransformPattern<CustomizeControlEventArgs, TAction>()
		        .TraceRX();
        
        public static IObservable<T2> WhenUpdating<T2>(this SimpleAction simpleAction,Func<UpdateActionEventArgs,IObservable<T2>> selector) 
            => simpleAction.Controller.WhenActivated()
                .SelectManyUntilDeactivated(controller => controller.Frame.GetController<ActionsCriteriaViewController>()
                    .WhenEvent<UpdateActionEventArgs>(nameof(ActionsCriteriaViewController.ActionUpdating)).Where(e => e.Active&&e.NeedUpdateEnabled)
                    .SelectMany(selector));

    }
}