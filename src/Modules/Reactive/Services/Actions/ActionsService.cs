using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Utils;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
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

        public static IObservable<T> WhenExecute<T>(this IObservable<SimpleAction> source,Func<SimpleActionExecuteEventArgs, IObservable<T>> retriedExecution) 
            => source.SelectMany(action => action.WhenExecute(retriedExecution).TakeUntilDeactivated(action.Controller));
        public static IObservable<T> WhenExecuted<T>(this IObservable<SimpleAction> source,Func<SimpleActionExecuteEventArgs, IObservable<T>> retriedExecution) 
            => source.SelectMany(action => action.WhenExecuted(retriedExecution).TakeUntilDeactivated(action.Controller));
        public static IObservable<Unit> WhenExecuted(this IObservable<SimpleAction> source,Action<SimpleActionExecuteEventArgs> retriedExecution) 
            => source.SelectMany(action => action.WhenExecuted(args => {
                retriedExecution(args);
                return Unit.Default.Observe();
            }));
        public static IObservable<Unit> WhenExecuted(this IObservable<SingleChoiceAction> source,Action<SingleChoiceActionExecuteEventArgs> retriedExecution) 
            => source.SelectMany(action => action.WhenExecuted(args => {
                retriedExecution(args);
                return Unit.Default.Observe();
            }));
        
        public static IObservable<Unit> WhenExecuted(this IObservable<ParametrizedAction> source,Action<ParametrizedActionExecuteEventArgs> retriedExecution) 
            => source.SelectMany(action => action.WhenExecuted(args => {
                retriedExecution(args);
                return Unit.Default.Observe();
            }));
        
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
            => simpleAction.WhenExecuted().SelectMany(e => retriedExecution(e).Retry(() => simpleAction.Application).TakeUntilDeactivated(simpleAction.Controller));

        public static IObservable<T> WhenExecuted<T>(this ParametrizedAction simpleAction, Func<ParametrizedActionExecuteEventArgs, IObservable<T>> retriedExecution)
            => simpleAction.WhenExecuted().SelectMany(e => retriedExecution(e).Retry(() => simpleAction.Application).TakeUntilDeactivated(simpleAction.Controller));
        
        public static IObservable<Unit> WhenExecuted(this SimpleAction simpleAction,Action<SimpleActionExecuteEventArgs> retriedExecution) 
            => simpleAction.WhenExecuted(e => {
                retriedExecution(e);
                return Observable.Empty<Unit>();
            });

        public static IObservable<Unit> WhenConcatExecution(this SimpleAction action,Action<SimpleActionExecuteEventArgs> sourceSelector) 
            => action.AsSimpleAction().WhenExecuted().SelectMany(e => e.WhenConcatExecution(pe => {
                sourceSelector(pe);
                return Observable.Empty<Unit>();
            }));

        public static IObservable<T> WhenConcatRetriedExecution<T>(this SimpleAction simpleAction,Func<SimpleActionExecuteEventArgs, IObservable<T>> retriedExecution)
            => simpleAction.WhenExecuted(e => {
                e.Action.Enabled[nameof(WhenConcatExecution)] = false;
                return retriedExecution.Invoke(e).ObserveOnContext().Finally(() => e.Action.Enabled[nameof(WhenConcatExecution)] = true);
            });

        public static IObservable<T> WhenConcatExecution<T>(this IObservable<SimpleAction> source, Func<SimpleActionExecuteEventArgs, IObservable<T>> sourceSelector)
            => source.SelectMany(action => action.WhenConcatExecution(sourceSelector));
        
        public static IObservable<T> WhenConcatExecution<T>(this SimpleAction simpleAction,Func<SimpleActionExecuteEventArgs,IObservable<T>> sourceSelector)
            => simpleAction.WhenExecuted().SelectMany(e => e.WhenConcatExecution(sourceSelector));

        private static IObservable<T> WhenConcatExecution<T,TArgs>(this TArgs e,Func<TArgs, IObservable<T>> sourceSelector) where TArgs:ActionBaseEventArgs{
            e.Action.Enabled[nameof(WhenConcatExecution)] = false;
            return sourceSelector(e).TakeUntilDisposed(e.Action).ObserveOnContext()
                .Finally(() => {
                    e.Action.Enabled[nameof(WhenConcatExecution)] = true;
                    e.Action.ExecutionFinished();
                });
        }

        public static IObservable<Unit> WhenConcatExecution(this ParametrizedAction action,Action<ParametrizedActionExecuteEventArgs> sourceSelector) 
            => action.AsParametrizedAction().WhenExecuted().SelectMany(e => e.WhenConcatExecution(pe => {
                sourceSelector(pe);
                return Observable.Empty<Unit>();
            }));

        public static IObservable<T> WhenConcatRetriedExecution<T>(this ParametrizedAction simpleAction,Func<ParametrizedActionExecuteEventArgs, IObservable<T>> sourceSelector)
            => simpleAction.WhenExecuted().SelectMany(e => e.WhenConcatExecution(sourceSelector));
        public static IObservable<T> WhenConcatExecution<T>(this ParametrizedAction simpleAction,Func<ParametrizedActionExecuteEventArgs,IObservable<T>> sourceSelector)
            => simpleAction.WhenConcatExecution<T,ParametrizedActionExecuteEventArgs>( sourceSelector);
        
        public static IObservable<T> WhenConcatExecution<T>(this SingleChoiceAction simpleAction,Func<SingleChoiceActionExecuteEventArgs,IObservable<T>> sourceSelector)
            => simpleAction.WhenConcatExecution<T,SingleChoiceActionExecuteEventArgs>( sourceSelector);
        
        private static IObservable<T> WhenConcatExecution<T,TArgs>(this ActionBase action, Func<TArgs, IObservable<T>> sourceSelector)  where TArgs:ActionBaseEventArgs 
            => action.WhenExecuted().Do(_ => action.Update(_ => action.Enabled[nameof(WhenConcatExecution)] = false)).Cast<TArgs>()
                .SelectManySequential(e => sourceSelector(e).ObserveOnContext()
                    .Finally(() => action.Update(_ => action.Enabled[nameof(WhenConcatExecution)] = true)));

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
        public static IObservable<Unit> WhenConcatExecution(this IObservable<SingleChoiceAction> source,Action<SingleChoiceActionExecuteEventArgs> retriedExecution) 
        => source.SelectMany(action => action.WhenConcatExecution<Unit,SingleChoiceActionExecuteEventArgs>(args => {
            retriedExecution(args);
            return Unit.Default.Observe();
        }));
        
        public static IObservable<Unit> WhenConcatExecution(this IObservable<SimpleAction> source,Action<SimpleActionExecuteEventArgs> retriedExecution) 
            => source.SelectMany(action => action.WhenConcatExecution(retriedExecution));
        
        public static IObservable<Unit> WhenConcatExecution(this IObservable<SimpleAction> source,Func<SimpleActionExecuteEventArgs,IObservable<Unit>> retriedExecution) 
            => source.SelectMany(action => action.WhenConcatRetriedExecution(retriedExecution));
        
        public static IObservable<T> WhenConcatExecution<T>(this IObservable<ParametrizedAction> source,Func<ParametrizedActionExecuteEventArgs,IObservable<T>> selector) 
            => source.SelectMany(action => action.WhenConcatExecution(selector));
        
        public static IObservable<T> WhenConcatExecution<T>(this IObservable<SingleChoiceAction> source,Func<SingleChoiceActionExecuteEventArgs,IObservable<T>> selector) 
            => source.SelectMany(action => action.WhenConcatExecution(selector));

        public static IObservable<T> WhenExecuted<T>(this IObservable<ParametrizedAction> source,Func<ParametrizedActionExecuteEventArgs, IObservable<T>> retriedExecution) 
            => source.SelectMany(action => action.WhenExecuted(retriedExecution));
        
        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecute(this IObservable<PopupWindowShowAction> source) 
            => source.SelectMany(action => action.WhenExecute());

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecute(this SimpleAction simpleAction) 
            => simpleAction.WhenEvent<SimpleActionExecuteEventArgs>(nameof(SimpleAction.Execute)).TakeUntilDisposed(simpleAction);
        
        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecute(this PopupWindowShowAction action) 
            => action.WhenEvent<PopupWindowShowActionExecuteEventArgs>(nameof(PopupWindowShowAction.Execute)).TakeUntilDisposed(action);
        
        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecute(this ParametrizedAction action) 
            => action.WhenEvent<ParametrizedActionExecuteEventArgs>(nameof(ParametrizedAction.Execute)).TakeUntilDisposed(action);

        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecute(this SingleChoiceAction singleChoiceAction) 
            => singleChoiceAction.WhenEvent<SingleChoiceActionExecuteEventArgs>(nameof(SingleChoiceAction.Execute));

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
        public static IObservable<(TAction action, CancelEventArgs e)> WhenExecuting<TAction>(this TAction action) where TAction : ActionBase 
            => action.WhenEvent<CancelEventArgs>(nameof(ActionBase.Executing)).InversePair(action).TakeUntilDisposed(action);

        public static  IObservable<(TAction action, Type objectType, View view, Frame frame, IObjectSpace objectSpace, ShowViewParameters showViewParameters)> ToParameter<TAction>(
                this IObservable<(TAction action, ActionBaseEventArgs e)> source) where TAction : ActionBase => source.Select(t => {
		        var frame = t.action.Controller.Frame;
		        return (t.action, frame.View.ObjectTypeInfo.Type, frame.View, frame, frame.View.ObjectSpace, t.e.ShowViewParameters);
	        });

        public static IObservable<TAction> ToAction<TAction>(this IObservable<(TAction action, ActionBaseEventArgs e)> source) where TAction : ActionBase 
            => source.Select(t => t.action);

        public static IObservable<CustomizePopupWindowParamsEventArgs> WhenCustomizePopupWindowParams(this PopupWindowShowAction action) 
            => action.WhenEvent<CustomizePopupWindowParamsEventArgs>(nameof(PopupWindowShowAction.CustomizePopupWindowParams))
                .TakeUntilDisposed(action);

        public static IObservable<SingleChoiceAction> AddItems(this IObservable<SingleChoiceAction> source,Func<SingleChoiceAction,IObservable<Unit>> addItems,IScheduler scheduler=null)
            => source.MergeIgnored(action => action.Controller.WhenActivated()
                .SelectMany(_ => action.View().WhenCurrentObjectChanged().StartWith(action.View()).TakeUntilDisposed(action))
                .WaitUntilInactive(1,scheduler:scheduler).ObserveOnContext()
                .Do(_ => action.Items.Clear()).SelectMany(_ => addItems(action)).TakeUntilDisposed(action));

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

        public static IObservable<SimpleActionExecuteEventArgs> WhenExecuted(this SimpleAction action) 
            => action.WhenEvent<ActionBaseEventArgs>(nameof(SimpleAction.Executed)).Cast<SimpleActionExecuteEventArgs>().TakeUntilDisposed(action);
        
        public static IObservable<SimpleActionExecuteEventArgs> WhenExecuteCompleted(this SimpleAction action) 
            => action.WhenEvent<ActionBaseEventArgs>(nameof(ActionBase.ExecuteCompleted)).Cast<SimpleActionExecuteEventArgs>().TakeUntilDisposed(action);
        
        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecuted(this SingleChoiceAction action) 
            => action.WhenEvent<ActionBaseEventArgs>(nameof(SingleChoiceAction.Executed)).Cast<SingleChoiceActionExecuteEventArgs>().TakeUntilDisposed(action);
        
        public static IObservable<SingleChoiceActionExecuteEventArgs> WhenExecuteCompleted(this SingleChoiceAction action) 
            => action.WhenEvent<ActionBaseEventArgs>(nameof(ActionBase.ExecuteCompleted)).Cast<SingleChoiceActionExecuteEventArgs>().TakeUntilDisposed(action);

        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecuted(this ParametrizedAction action) 
            => action.WhenEvent<ActionBaseEventArgs>(nameof(ParametrizedAction.Executed)).Cast<ParametrizedActionExecuteEventArgs>().TakeUntilDisposed(action);
        
        public static IObservable<ParametrizedActionExecuteEventArgs> WhenExecuteCompleted(this ParametrizedAction action) 
            => action.WhenEvent<ActionBaseEventArgs>(nameof(ActionBase.ExecuteCompleted)).Cast<ParametrizedActionExecuteEventArgs>().TakeUntilDisposed(action);

        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecuted(this PopupWindowShowAction action) 
            => action.WhenEvent<ActionBaseEventArgs>(nameof(PopupWindowShowAction.Executed)).Cast<PopupWindowShowActionExecuteEventArgs>().TakeUntilDisposed(action);
        
        public static IObservable<PopupWindowShowActionExecuteEventArgs> WhenExecuteCompleted(this PopupWindowShowAction action) 
            => action.WhenEvent<ActionBaseEventArgs>(nameof(ActionBase.ExecuteCompleted)).Cast<PopupWindowShowActionExecuteEventArgs>().TakeUntilDisposed(action);

        public static IObservable<ActionBaseEventArgs> WhenExecuted<TAction>(this TAction action) where TAction : ActionBase 
            => action.WhenEvent<ActionBaseEventArgs>(nameof(ActionBase.Executed)).TakeUntilDisposed(action);

        public static IObservable<ItemsChangedEventArgs> WhenItemsChanged(this SingleChoiceAction action,ChoiceActionItemChangesType? changesType=null) 
            => action.WhenEvent<ItemsChangedEventArgs>(nameof(SingleChoiceAction.ItemsChanged))
                .Where(e =>changesType==null||e.ChangedItemsInfo.Any(pair => pair.Value==changesType) ).TakeUntil(action.WhenDisposed());
        
        public static IObservable<ActionBaseEventArgs> WhenExecuteCompleted<TAction>(this TAction action) where TAction : ActionBase 
            => action.WhenEvent<ActionBaseEventArgs>(nameof(ActionBase.ExecuteCompleted)).TakeUntilDisposed(action);
        
        public static IObservable<TAction> WhenExecuteConcat<TAction>(this TAction action) where TAction : ActionBase 
            => action.WhenDisabled().Where(a => a.Enabled.Contains(nameof(WhenConcatExecution)))
                .SelectMany(a => a.WhenEnabled().Where(ab1 => ab1.Enabled.Contains(nameof(WhenConcatExecution))).To(a));

        private static readonly ISubject<ActionBase> ExecuteFinishedSubject = Subject.Synchronize(new Subject<ActionBase>());
        public static IObservable<TAction> WhenExecuteFinished<TAction>(this TAction action,bool customEmit=false) where TAction : ActionBase
            => customEmit || (action.Data.ContainsKey(nameof(ExecutionFinished)) && (bool)action.Data[nameof(ExecutionFinished)])
                ? ExecuteFinishedSubject.Where(a => a == action).Cast<TAction>() : action.WhenExecuteCompleted().To(action);

        public static void CustomizeExecutionFinished<TAction>(this TAction action, bool enable=true) where TAction : ActionBase
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
        
        public static IObservable<T> TakeUntilDisposed<T>(this IObservable<T> source, ActionBase component) 
            => source.TakeWhileInclusive(_ => !component.IsDisposed);

        public static IObservable<(TAction action, BoolList boolList, BoolValueChangedEventArgs e)> ResultValueChanged<TAction>(
                this TAction source, Func<TAction, BoolList> boolListSelector) where TAction : ActionBase 
            => boolListSelector(source).Observe().ResultValueChanged().Select(tuple => (source, tuple.boolList, tuple.e));

        public static IObservable<SingleChoiceAction> WhenSelectedItemChanged(this IObservable<SingleChoiceAction> source) 
            =>source.SelectMany(action => action.WhenSelectedItemChanged());

        public static IObservable<SingleChoiceAction> WhenSelectedItemChanged(this SingleChoiceAction action) 
            => action.WhenEvent(nameof(SingleChoiceAction.SelectedItemChanged)).To(action);

        public static TAction As<TAction>(this ActionBase action) where TAction:ActionBase 
            => ((TAction) action);

        public static IObservable<Unit> WhenDisposing<TAction>(this TAction simpleAction) where TAction : ActionBase 
            => Disposing(simpleAction.Observe());

        public static IObservable<TAction> WhenControllerActivated<TAction>(this IObservable<TAction> source,bool emitWhenActive=false) where TAction : ActionBase 
            => source.SelectMany(a =>a.Controller.WhenActivated(emitWhenActive).To(a) );
        
        public static IObservable<TAction> WhenControllerActivated<TAction>(this IObservable<TAction> source,Func<TAction,IObservable<Unit>> mergeSelector,bool emitWhenActive=false) where TAction : ActionBase 
            => source.SelectMany(a =>a.Controller.WhenActivated(emitWhenActive).TakeUntil(a.Controller.WhenDeactivated())
                .SelectMany(_ => mergeSelector(a)).To(a) );
        
        public static IObservable<TAction> WhenControllerDeActivated<TAction>(this IObservable<TAction> source,bool emitWhenActive=false) where TAction : ActionBase 
            => source.SelectMany(a =>a.Controller.WhenDeactivated().To(a) );

        public static IObservable<TAction> WhenActive<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.Where(a => a.Active);
        public static IObservable<TAction> WhereAvailable<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.Where(a => a.Available());

        public static IObservable<TAction> WhenInActive<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.Where(a => !a.Active);

        public static IObservable<TAction> WhenActive<TAction>(this TAction simpleAction) where TAction : ActionBase 
            => simpleAction.Observe().WhenActive();

		public static IObservable<TAction> WhenActivated<TAction>(this IObservable<TAction> source,params string[] contexts) where TAction : ActionBase 
            => source.SelectMany(a => a.WhenActivated());
		
		public static IObservable<TAction> WhenInActive<TAction>(this TAction simpleAction) where TAction : ActionBase 
            => simpleAction.Observe().WhenInActive();

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
        
        public static IObservable<TAction> WhenDisabled<TAction>(this TAction simpleAction,params string[] contexts) where TAction : ActionBase 
            => simpleAction.ResultValueChanged(action => action.Enabled)
		        .Where(tuple => !tuple.action.Enabled.ResultValue)
                .SelectMany(t =>contexts.Any()? contexts.Where(context => t.action.Enabled.Contains(context)&&!t.action.Enabled[context]).To(t):t.YieldItem())
		        .Select(t => t.action);

        public static IObservable<TAction> WhenChanged<TAction>(this IObservable<TAction> source,ActionChangedType? actionChangedType = null)where TAction : ActionBase 
            => source.SelectMany(a => a.WhenChanged(actionChangedType));

        public static IObservable<TAction> WhenChanged<TAction>(this TAction action, ActionChangedType? actionChangedType = null) where TAction : ActionBase 
            => action.WhenEvent<ActionChangedEventArgs>(nameof(ActionBase.Changed))
		        .Where(eventArgs =>actionChangedType==null|| eventArgs.ChangedPropertyType == actionChangedType).To(action);

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
            => source .SelectMany(item => item.WhenEvent(nameof(ActionBase.Disposing)).ToUnit());

        public static IObservable<ParametrizedAction> WhenValueChanged(this ParametrizedAction action)
            => action.WhenEvent(nameof(ParametrizedAction.ValueChanged)).TakeUntilDisposed(action).Select(pattern => (ParametrizedAction)pattern.Sender);
        
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

        public static IObservable<CustomizeControlEventArgs> WhenCustomizeControl<TAction>(this TAction action) where TAction:ActionBase 
            => action.WhenEvent<CustomizeControlEventArgs>(nameof(ActionBase.CustomizeControl));
        
        public static IObservable<T2> WhenUpdating<T2>(this ActionBase action,Func<UpdateActionEventArgs,IObservable<T2>> selector) 
            => action.Controller.WhenActivated(true)
                .SelectManyUntilDeactivated(controller => controller.Frame.GetController<ActionsCriteriaViewController>()
                    .WhenEvent<UpdateActionEventArgs>(nameof(ActionsCriteriaViewController.ActionUpdating)).Where(e => e.Active&&e.NeedUpdateEnabled)
                    .SelectMany(selector));

        public static IObservable<DialogController> CreateDialogController(this ActionBaseEventArgs e,ObjectView objectView,string caption=null,bool refreshViewAfterObjectSpaceCommit=true,bool closeOnCancel=true){
            var application = e.Application();
            var parameters = e.ShowViewParameters;
            parameters.TargetWindow = TargetWindow.NewModalWindow;
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
                .DoWhen(_ => refreshViewAfterObjectSpaceCommit,frame => frame.GetController<ModificationsController>().SetPropertyValue("RefreshViewAfterObjectSpaceCommit",false))
                .IgnoreElements().To<DialogController>().StartWith(dialogController)
                .If(_ => closeOnCancel,controller => controller.CancelAction.WhenExecuted(_ => e.ShowViewParameters.CreatedView.Close())
                    .IgnoreElements().To<DialogController>().StartWith(dialogController));
        }

        
        public static IObservable<T> Trigger<T>(this SingleChoiceAction action, IObservable<T> afterExecuted,params object[] selection)
            => action.Trigger(afterExecuted,() => action.SelectedItem,selection);
        
        public static IObservable<Unit> Trigger(this SingleChoiceAction action, params object[] selection)
            => action.Trigger(Observable.Empty<Unit>(),() => action.SelectedItem,selection);
        
        public static IObservable<T> Trigger<T>(this SingleChoiceAction action, IObservable<T> afterExecuted,Func<ChoiceActionItem> selectedItem,params object[] selection)
            => afterExecuted.Trigger(() => action.DoExecute(selectedItem(), selection));
        public static IObservable<T> Trigger<T>(this PopupWindowShowAction action, IObservable<T> afterExecuted,Window window) {
            return afterExecuted.Trigger(() => action.DoExecute(window));
        }

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
        
        public static IObservable<Unit> Trigger(this SimpleAction action, params object[] selection)
            => action.Trigger(action.WhenExecuteCompleted().ToUnit(),selection);

    }
}