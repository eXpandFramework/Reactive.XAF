using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.CriteriaOperatorExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.Reactive.Services.Actions {
    public static partial class ActionsService {
        public static IObservable<T> WhenAvailable<T>(this IObservable<T> source) where T:ActionBase 
            => source.Where(action => action.Available());

        public static IObservable<T> SetImage<T,TObject>(this IObservable<T> source, CommonImage startImage,
            CommonImage replaceImage, Expression<Func<TObject, bool>> lambda) where T : ActionBase
            => source.MergeIgnored(action => action.SetImage(startImage, replaceImage, lambda))
                .PushStackFrame();
        
        public static IObservable<T> SetImage<T>(this ActionBase action, CommonImage startImage,
            CommonImage replaceImage, Expression<Func<T, bool>> lambda)  
            => action.Controller.WhenActivated().Do(_ => action.SetImage( lambda,startImage, replaceImage)).Select(_ => action.View())
                .SelectMany(view => view.ObjectSpace.WhenModifiedObjects<T>()
                    .Merge(view.WhenSelectionChanged().SelectMany(_ => view.SelectedObjects.Cast<T>()))
                    .StartWith(view.SelectedObjects.Cast<T>())
                    .WaitUntilInactive(TimeSpan.FromMilliseconds(250)).ObserveOnContext()
                .Do(_ => action.SetImage( lambda,startImage, replaceImage)))
                .PushStackFrame();

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

        public static IObservable<TAction> WhenControllerActivated<TAction>(this IObservable<TAction> source,bool emitWhenActive=false) where TAction : ActionBase 
            => source.SelectMany(a =>a.Controller.WhenActivated(emitWhenActive).To(a) ) ;
        
        public static IObservable<TAction> ActivateFor<TAction>(this IObservable<TAction> source,TemplateContext context) where TAction : ActionBase
            => source.WhenControllerActivated(action => action.Observe()
                .Do(simpleAction => action.Active[$"{nameof(ActivateFor)} {context}"]=simpleAction.Controller.Frame.Context==context).ToUnit())
                .PushStackFrame();
        
        public static IObservable<TAction> ActivateFor<TAction>(this IObservable<TAction> source,Func<TAction,bool> condition) where TAction : ActionBase
            => source.WhenControllerActivated(action => action.Observe()
                .Do(simpleAction => simpleAction.Active[$"{nameof(ActivateFor)}"]=condition(simpleAction)).ToUnit())
                .PushStackFrame();
        
        public static IObservable<TAction> EnableFor<TAction>(this IObservable<TAction> source,Func<TAction,bool> condition) where TAction : ActionBase
            => source.WhenControllerActivated(action => action.View().WhenSelectedObjectsChanged().To(action).StartWith(action)
                .Do(simpleAction => simpleAction.Enabled[$"{nameof(EnableFor)}"]=condition(simpleAction)).ToUnit())
                .PushStackFrame();
        
        public static IObservable<TAction> WhenControllerDeActivated<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.SelectMany(a =>a.Controller.WhenDeactivated().To(a) )
;

        public static IObservable<TAction> WhenActive<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.Where(a => a.Active);
        
        public static IObservable<TAction> WhereAvailable<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.Where(a => a.Available());

        public static IObservable<TAction> WhenInActive<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.Where(a => !a.Active);

        public static IObservable<TAction> WhenActive<TAction>(this TAction simpleAction) where TAction : ActionBase 
            => simpleAction.Observe().WhenActive();

       public static IObservable<TAction> WhenActivated<TAction>(this IObservable<TAction> source,string[] contexts=null,[CallerMemberName]string caller="") where TAction : ActionBase 
            => source.SelectMany(a => a.WhenActivated(contexts,caller))
                .PushStackFrame();
       
       public static IObservable<TAction> WhenInActive<TAction>(this TAction simpleAction) where TAction : ActionBase 
            => simpleAction.Observe().WhenInActive();

       public static IObservable<TAction> WhenDeactivated<TAction>(this IObservable<TAction> source) where TAction : ActionBase 
            => source.SelectMany(a => a.WhenDeactivated())
                .PushStackFrame();

        public static IObservable<TAction> WhenActivated<TAction>(this TAction simpleAction,string[] contexts=null,[CallerMemberName]string caller="") where TAction : ActionBase 
            => simpleAction.ResultValueChanged(action => action.Active,caller:caller)
                .SelectManyItemResilient(t => (contexts ??[]).Concat(Controller.ControllerActiveKey.YieldItem()).Select(context => (t,context))
                    .Where(t1 => t1.t.action.Active.ResultValue&&t1.t.action.Active.Contains(t1.context)&& t1.t.action.Active[t1.context])
                    .Select(t1 => t1.t.action))
                
                .PushStackFrame();
        
        public static IObservable<TAction> WhenDeactivated<TAction>(this TAction simpleAction) where TAction : ActionBase 
            => simpleAction.ResultValueChanged(action => action.Active)
               .Where(tuple => !tuple.action.Active.ResultValue)
               .Select(t => t.action)
               .PushStackFrame();
        
        public static IObservable<TAction> WhenDisabled<TAction>(this TAction simpleAction,params string[] contexts) where TAction : ActionBase 
            => simpleAction.ResultValueChanged(action => action.Enabled)
               .Where(tuple => !tuple.action.Enabled.ResultValue)
                .SelectMany(t =>contexts.Any()? contexts.Where(context => t.action.Enabled.Contains(context)&&!t.action.Enabled[context]).To(t):t.YieldItem())
               .Select(t => t.action);
        
        public static IObservable<TAction> WhenEnable<TAction>(this IObservable<TAction> source)where TAction : ActionBase 
            => source.Where(a => a.Enabled);

        public static IObservable<TAction> WhenEnable<TAction>(this TAction simpleAction) where TAction : ActionBase 
            =>simpleAction.Observe().WhenEnable();

        public static IObservable<TAction> WhenEnabled<TAction>(this IObservable<TAction> source)where TAction : ActionBase 
            => source.SelectMany(a => a.WhenEnabled());

        public static IObservable<TAction> WhenEnabled<TAction>(this TAction simpleAction) where TAction : ActionBase 
            =>simpleAction.ResultValueChanged(action => action.Enabled).Where(tuple => tuple.action.Enabled.ResultValue)
                .Select(t => t.action);
        
        public static IObservable<TAction> ActivateInLookupListView<TAction>(this IObservable<TAction> source) where TAction:ActionBase
            => source.WhenControllerActivated().Do(action => action.Active[nameof(ActivateInLookupListView)]=action.Frame().Template is ILookupPopupFrameTemplate)
                .PushStackFrame();
        
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
                .PushStackFrame();
    }
}