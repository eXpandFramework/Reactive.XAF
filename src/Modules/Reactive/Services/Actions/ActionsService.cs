using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Utils;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.FrameExtensions;

namespace Xpand.XAF.Modules.Reactive.Services.Actions{
    
    public static partial class ActionsService{
	    public static IObservable<(TModule module, Frame frame)> Action<TModule>(
		    this IObservable<Frame> source) where TModule : ModuleBase =>
		    source.Select(frame => frame.Action<TModule>());

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecute(this IObservable<SimpleAction> source) => source.SelectMany(action => action.WhenExecute());

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecute(this SimpleAction simpleAction) =>
	        Observable.FromEventPattern<SimpleActionExecuteEventHandler, SimpleActionExecuteEventArgs>(
			        h => simpleAction.Execute += h, h => simpleAction.Execute -= h, ImmediateScheduler.Instance)
		        .Select(pattern => pattern.EventArgs);

        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecute(this SingleChoiceAction simpleAction) =>
	        Observable.FromEventPattern<SingleChoiceActionExecuteEventHandler, SingleChoiceActionExecuteEventArgs>(
			        h => simpleAction.Execute += h, h => simpleAction.Execute -= h, ImmediateScheduler.Instance)
		        .Select(pattern => pattern.EventArgs);

        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecute(this IObservable<SingleChoiceAction> source) => source.SelectMany(action => action.WhenExecute());

        public static IObservable<TFrame> WhenView<TFrame>(this IObservable<TFrame> source, Type objectType) where TFrame : Frame => source.SelectMany(frame => frame.View.ReturnObservable().When(objectType).Select(view => frame));

        public static IObservable<TAction> When<TAction>(this IObservable<TAction> source, Type objectType) where TAction : ActionBase => source.Where(_ => objectType.IsAssignableFrom(_.Controller.Frame.View.ObjectTypeInfo.Type));

        public static IObservable<IObjectSpace> ToObjectSpace<TAction>(this IObservable<TAction> source) where TAction : ActionBase => source.Select(_ => _.Controller.Frame.View.ObjectSpace);

        public static IObservable<(TAction action, CancelEventArgs e)> WhenExecuting<TAction>(this TAction action) where TAction : ActionBase =>
	        Observable.FromEventPattern<CancelEventHandler, CancelEventArgs>(h => action.Executing += h,
		        h => action.Executing -= h, ImmediateScheduler.Instance).TransformPattern<CancelEventArgs, TAction>();

        public static  IObservable<(TAction action, Type objectType, View view, Frame frame, IObjectSpace objectSpace,
                ShowViewParameters showViewParameters)> ToParameter<TAction>(
                this IObservable<(TAction action, ActionBaseEventArgs e)> source) where TAction : ActionBase => source.Select(_ => {
		        var frame = _.action.Controller.Frame;
		        return (_.action, frame.View.ObjectTypeInfo.Type, frame.View, frame, frame.View.ObjectSpace, _.e.ShowViewParameters);
	        });

        public static IObservable<TAction> ToAction<TAction>(this IObservable<(TAction action, ActionBaseEventArgs e)> source) where TAction : ActionBase => source.Select(_ => _.action);

        public static IObservable<(PopupWindowShowAction action, CustomizePopupWindowParamsEventArgs e)> WhenCustomizePopupWindowParams(this PopupWindowShowAction action) =>
	        Observable.FromEventPattern<CustomizePopupWindowParamsEventHandler, CustomizePopupWindowParamsEventArgs>(
			        h => action.CustomizePopupWindowParams += h, h => action.CustomizePopupWindowParams -= h, ImmediateScheduler.Instance)
		        .TransformPattern<CustomizePopupWindowParamsEventArgs, PopupWindowShowAction>();

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecuted(this SimpleAction action) =>
	        Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
		        h => action.Executed += h, h => action.Executed -= h, ImmediateScheduler.Instance).Select(_ =>(SimpleActionExecuteEventArgs) _.EventArgs);

        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecuted(this SingleChoiceAction action) =>
	        Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
		        h => action.Executed += h, h => action.Executed -= h, ImmediateScheduler.Instance).Select(_ => (SingleChoiceActionExecuteEventArgs)_.EventArgs);

        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecuted(this ParametrizedAction action) =>
	        Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
		        h => action.Executed += h, h => action.Executed -= h, ImmediateScheduler.Instance).Select(_ => (ParametrizedActionExecuteEventArgs)_.EventArgs);

        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecuted(this PopupWindowShowAction action) =>
	        Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
		        h => action.Executed += h, h => action.Executed -= h, ImmediateScheduler.Instance).Select(_ => (PopupWindowShowActionExecuteEventArgs)_.EventArgs);

        public static IObservable<(TAction action, ActionBaseEventArgs e)> WhenExecuted<TAction>(this TAction action) where TAction : ActionBase =>
	        Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
			        h => action.Executed += h, h => action.Executed -= h, ImmediateScheduler.Instance)
		        .TransformPattern<ActionBaseEventArgs, TAction>();

        public static IObservable<(TAction action, ActionBaseEventArgs e)> WhenExecuteCompleted<TAction>(this TAction action) where TAction : ActionBase =>
	        Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
			        h => action.ExecuteCompleted += h, h => action.ExecuteCompleted -= h, ImmediateScheduler.Instance)
		        .TransformPattern<ActionBaseEventArgs, TAction>();

        public static IObservable<(SimpleAction action, SimpleActionExecuteEventArgs e)> WhenExecuteCompleted(this SimpleAction action) =>
	        Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
			        h => action.ExecuteCompleted += h, h => action.ExecuteCompleted -= h, ImmediateScheduler.Instance)
		        .Select(pattern => ((SimpleAction)pattern.Sender,(SimpleActionExecuteEventArgs)pattern.EventArgs));

        public static IObservable<(SingleChoiceAction action, SingleChoiceActionExecuteEventArgs e)> WhenExecuteCompleted(this SingleChoiceAction action) =>
	        Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
			        h => action.ExecuteCompleted += h, h => action.ExecuteCompleted -= h, ImmediateScheduler.Instance)
		        .Select(pattern => ((SingleChoiceAction)pattern.Sender,(SingleChoiceActionExecuteEventArgs)pattern.EventArgs));

        public static IObservable<(ParametrizedAction action, ParametrizedActionExecuteEventArgs e)> WhenExecuteCompleted(this ParametrizedAction action) =>
	        Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
			        h => action.ExecuteCompleted += h, h => action.ExecuteCompleted -= h, ImmediateScheduler.Instance)
		        .Select(pattern => ((ParametrizedAction)pattern.Sender,(ParametrizedActionExecuteEventArgs)pattern.EventArgs));

        public static IObservable<(PopupWindowShowAction action, PopupWindowShowActionExecuteEventArgs e)> WhenExecuteCompleted(this PopupWindowShowAction action) =>
	        Observable.FromEventPattern<EventHandler<ActionBaseEventArgs>, ActionBaseEventArgs>(
			        h => action.ExecuteCompleted += h, h => action.ExecuteCompleted -= h, ImmediateScheduler.Instance)
		        .Select(pattern => ((PopupWindowShowAction)pattern.Sender,(PopupWindowShowActionExecuteEventArgs)pattern.EventArgs));

        public static IObservable<(TAction action, BoolList boolList, BoolValueChangedEventArgs e)> ResultValueChanged<TAction>(
                this TAction source, Func<TAction, BoolList> boolListSelector) where TAction : ActionBase =>
	        Observable.Return(boolListSelector(source))
		        .ResultValueChanged().Select(tuple => (source, tuple.boolList, tuple.e));

        public static TAction As<TAction>(this ActionBase action) where TAction:ActionBase => ((TAction) action);

        public static IObservable<Unit> WhenDisposing<TAction>(this TAction simpleAction) where TAction : ActionBase => Disposing(Observable.Return(simpleAction));

        public static IObservable<TAction> WhenActive<TAction>(this IObservable<TAction> source) where TAction : ActionBase => source.Where(a => a.Active);

        public static IObservable<TAction> WhenActive<TAction>(this TAction simpleAction) where TAction : ActionBase =>
	        simpleAction.ReturnObservable().WhenActive();
		public static IObservable<TAction> WhenActivated<TAction>(this IObservable<TAction> source) where TAction : ActionBase => source.SelectMany(a => a.WhenActivated());

        public static IObservable<TAction> WhenActivated<TAction>(this TAction simpleAction) where TAction : ActionBase =>
	        simpleAction.ResultValueChanged(action => action.Active)
		        .Where(tuple => tuple.action.Active.ResultValue)
		        .Select(_ => _.action);

        public static IObservable<TAction> WhenChanged<TAction>(this IObservable<TAction> source,ActionChangedType? actionChangedType = null)where TAction : ActionBase => source
	        .SelectMany(a => a.WhenChanged(actionChangedType));

        public static IObservable<TAction> WhenChanged<TAction>(this TAction action, ActionChangedType? actionChangedType = null) where TAction : ActionBase =>
	        Observable.FromEventPattern<EventHandler<ActionChangedEventArgs>, ActionChangedEventArgs>(
			        h => action.Changed += h, h => action.Changed -= h)
		        .Where(pattern =>actionChangedType==null|| pattern.EventArgs.ChangedPropertyType == actionChangedType)
		        .Select(pattern => pattern.Sender).Cast<TAction>();

        public static IObservable<TAction> WhenEnable<TAction>(this IObservable<TAction> source)where TAction : ActionBase => source.Where(a => a.Enabled);

        public static IObservable<TAction> WhenEnable<TAction>(this TAction simpleAction) where TAction : ActionBase =>simpleAction.ReturnObservable().WhenEnable();
        public static IObservable<TAction> WhenEnabled<TAction>(this IObservable<TAction> source)where TAction : ActionBase => source.SelectMany(a => a.WhenEnabled());

        public static IObservable<TAction> WhenEnabled<TAction>(this TAction simpleAction) where TAction : ActionBase =>simpleAction
	        .ResultValueChanged(action => action.Enabled)
	        .Where(tuple => tuple.action.Enabled.ResultValue)
	        .Select(_ => _.action);

        public static IObservable<Unit> Disposing<TAction>(this IObservable<TAction> source) where TAction : ActionBase =>
	        source .SelectMany(item => Observable.FromEventPattern<EventHandler, EventArgs>(h => item.Disposing += h,
			        h => item.Disposing -= h, ImmediateScheduler.Instance)
		        .Select(pattern => pattern).ToUnit());
    }
}