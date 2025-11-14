using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.FrameExtensions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Utils;
using Fasterflect;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.Reactive.Services.Actions {
    public static partial class ActionsService {

        public static IObservable<TAction> WhenControllerActivated<TAction>(this IObservable<TAction> source,Func<TAction,IObservable<Unit>> mergeSelector,bool emitWhenActive=false) where TAction : ActionBase 
            => source.MergeIgnored(a =>a.Controller.WhenActivated(emitWhenActive)
                    .TakeUntil(a.Controller.WhenDeactivated())
                .SelectManyItemResilient(_ => mergeSelector(a)).To(a) )
                .PushStackFrame();

        public static IObservable<T> CommitChanges<T>(this IObservable<T> source) where T : ActionBaseEventArgs
            => source.SelectManyItemResilient(e => e.Observe()
                        .Do(_ => e.Action.View()?.AsObjectView()?.ObjectSpace.CommitChanges()))
                .PushStackFrame();
        
        public static IObservable<SingleChoiceAction> AddItems(this IObservable<SingleChoiceAction> source,Func<SingleChoiceAction,IObservable<Unit>> addItemsResilient,IScheduler scheduler=null)
            => source.MergeIgnored(action => action.Controller.WhenActivated(emitWhenActive: true)
                .SelectMany(_ => action.View().WhenCurrentObjectChanged().StartWith(action.View()).TakeUntilDisposed(action))
                .WaitUntilInactive(1, scheduler: scheduler).ObserveOnContextMaybe()
                // .DoItemResilient(_ => action.Items.Clear())
                .SelectManyItemResilient(_ => addItemsResilient(action)
                    .PushStackFrame( )).TakeUntilDisposed(action))
                .PushStackFrame();

        public static IObservable<(TModule module, Frame frame)> Action<TModule>(
           this IObservable<Frame> source) where TModule : ModuleBase 
            => source.Select(frame => frame.Action<TModule>());
        
        public static IObservable<TFrame> WhenView<TFrame>(this IObservable<TFrame> source, Type objectType) where TFrame : Frame 
            => source.SelectMany(frame => frame.View.Observe().When(objectType).Select(_ => frame));
        
        public static IObservable<IObjectSpace> ToObjectSpace<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.Select(action => action.Controller.Frame.View.ObjectSpace);
        
        public static  IObservable<(TAction action, Type objectType, View view, Frame frame, IObjectSpace objectSpace, ShowViewParameters showViewParameters)> ToParameter<TAction>(
                this IObservable<(TAction action, ActionBaseEventArgs e)> source) where TAction : ActionBase => source.Select(t => {
               var frame = t.action.Controller.Frame;
               return (t.action, frame.View.ObjectTypeInfo.Type, frame.View, frame, frame.View.ObjectSpace, t.e.ShowViewParameters);
            });

        public static IObservable<TAction> ToAction<TAction>(this IObservable<(TAction action, ActionBaseEventArgs e)> source) where TAction : ActionBase 
            => source.Select(t => t.action);

        public static IObservable<TArgs> CreateDetailView<TArgs>(this IObservable<TArgs> source, Type objectType=null, TargetWindow? targetWindow =null) where TArgs:ActionBaseEventArgs
            => source.DoItemResilient(e => {
                    var parameters = e.ShowViewParameters;
                    objectType ??= e.Action.View().ObjectTypeInfo.Type;
                    parameters.CreatedView = e.Action.Application.NewDetailView(objectType);
                    parameters.CreatedView.CurrentObject = parameters.CreatedView.ObjectSpace.CreateObject(objectType);
                    if (targetWindow.HasValue) parameters.TargetWindow = targetWindow.Value;
                })
                .PushStackFrame();

        public static IObservable<T> TakeUntilDisposed<T>(this IObservable<T> source, ActionBase component) 
            => source.TakeUntil(component.WhenDisposed());

        public static IObservable<(TAction action, BoolList boolList, BoolValueChangedEventArgs e)> ResultValueChanged<TAction>(
                this TAction source, Func<TAction, BoolList> boolListSelector) where TAction : ActionBase 
            => boolListSelector(source).Observe().ResultValueChanged().Select(tuple => (source, tuple.boolList, tuple.e));

        public static IObservable<SingleChoiceAction> WhenSelectedItemChanged(this IObservable<SingleChoiceAction> source) 
            =>source.SelectMany(action => action.WhenSelectedItemChanged());

        public static IObservable<SingleChoiceAction> WhenSelectedItemChanged(this SingleChoiceAction action) 
            => action.ProcessEvent(nameof(SingleChoiceAction.SelectedItemChanged)).To(action);

        public static TAction As<TAction>(this ActionBase action) where TAction:ActionBase 
            => ((TAction) action);
        
        public static IObservable<DialogController> CreateDialogController(this ActionBaseEventArgs e,ObjectView objectView,string caption=null,bool refreshViewAfterObjectSpaceCommit=true,bool closeOnCancel=true,TargetWindow targetWindow=TargetWindow.NewModalWindow){
            return e.DeferItemResilient(() => {
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
                        .IgnoreElements().To<DialogController>().StartWith(dialogController))
                    .PushStackFrame();
            });
        }

        public static IObservable<Frame> LinkObject(this PopupWindowShowAction action) 
            => action.Application.WhenFrame().When(TemplateContext.LookupWindowContextName).Take(1)
                .If(frame => ((ILookupPopupFrameTemplate)frame.Template).IsSearchEnabled,frame => frame.GetController<FilterController>().FullTextFilterAction
                    .Trigger(frame.View.WhenObjects().Take(1).To(frame)),frame => frame.View.WhenObjects().Take(1).To(frame) ).IgnoreElements()
                .Merge(action.Trigger(action.WhenExecuteCompleted().To(action.Frame())))
                .PushStackFrame();
    }
}