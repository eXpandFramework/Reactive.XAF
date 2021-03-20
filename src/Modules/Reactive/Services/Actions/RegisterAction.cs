using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Reflection.Emit;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Core;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using HarmonyLib;
using JetBrains.Annotations;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.XAF.Modules.Reactive.Services.Actions{
    [PublicAPI]
    public static partial class ActionsService{
	    static readonly ConcurrentDictionary<Type, (string id, Func<(Controller controller, string id), ActionBase> actionBase)> ControllerCtorState;
        static ActionsService(){
	        ControllerCtorState = new ConcurrentDictionary<Type, (string id, Func<(Controller controller, string id), ActionBase> actionBase)>();
	        var dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ActionsAssembly"),AssemblyBuilderAccess.Run);
            ActionsModule = dynamicAssembly.DefineDynamicModule("ActionsModule");
            // AppDomain.CurrentDomain.Patch(harmony => {
            //     var original = typeof(Controller).Method(nameof(SetInfo));
            //     harmony.Patch(original, postfix: new HarmonyMethod(typeof(ActionsService), nameof(SetInfo)));
            // });
        }

        public static void SetInfo(Controller destController, IModelApplication modelApplication) {

        }
        private static ModuleBuilder ActionsModule{ get; }

        public static IObservable<TAction> RegisterMainWindowAction<TAction>(this ApplicationModulesManager applicationModulesManager, string id,
            Func<(WindowController controller, string id), TAction> actionBase) where  TAction:ActionBase 
	        => applicationModulesManager.RegisterWindowAction(id, tuple => {
		        var action = actionBase(tuple);
		        tuple.controller.TargetWindowType=WindowType.Child;
		        return action;
	        });

        public static IObservable<TAction> RegisterChildWindowAction<TAction>(this ApplicationModulesManager applicationModulesManager, string id,
            Func<(WindowController controller, string id), TAction> actionBase) where  TAction:ActionBase 
	        => applicationModulesManager.RegisterWindowAction(id, tuple => {
                    var action = actionBase(tuple);
                    tuple.controller.TargetWindowType=WindowType.Child;
                    return action;
                });

        public static IObservable<TAction> RegisterWindowAction<TAction>(this ApplicationModulesManager applicationModulesManager, string id,
            Func<(WindowController controller, string id), TAction> actionBase) where TAction:ActionBase 
	        => applicationModulesManager.RegisterAction(id, actionBase);

        public static IObservable<PopupWindowShowAction> RegisterViewPopupWindowShowAction(this ApplicationModulesManager manager, string id,
            Type targetObjectType=null,ViewType viewType=ViewType.Any,PredefinedCategory category=PredefinedCategory.View) 
	        => manager.RegisterViewAction(id, category.NewAction<PopupWindowShowAction,ViewController>(targetObjectType.Configure(viewType)));

        public static IObservable<PopupWindowShowAction> RegisterViewPopupWindowShowAction(this ApplicationModulesManager manager, string id,
            Action<PopupWindowShowAction> configure,PredefinedCategory category=PredefinedCategory.View) 
	        => manager.RegisterViewAction(id, category.NewAction<PopupWindowShowAction,ViewController>(configure));

        public static IObservable<PopupWindowShowAction> RegisterWindowPopupWindowShowAction(this ApplicationModulesManager manager, string id,
            PredefinedCategory category=PredefinedCategory.View,Action<PopupWindowShowAction> configure=null) 
	        => manager.RegisterWindowAction(id, category.NewAction<PopupWindowShowAction,WindowController>(configure));

        public static IObservable<SimpleAction> RegisterViewSimpleAction(this ApplicationModulesManager manager, string id, 
            Type targetObjectType=null,ViewType viewType=ViewType.Any,PredefinedCategory category = PredefinedCategory.View) 
                => manager.RegisterViewAction(id, category.NewAction<SimpleAction,ViewController>( targetObjectType.Configure(viewType)));


        public static IObservable<SimpleAction> RegisterViewSimpleAction(this ApplicationModulesManager manager, string id, 
            Action<SimpleAction> configure,PredefinedCategory category = PredefinedCategory.View) 
                => manager.RegisterViewAction(id, category.NewAction<SimpleAction,ViewController>( configure));

        public static IObservable<SingleChoiceAction> RegisterViewSingleChoiceAction(this ApplicationModulesManager manager, string id, 
            Type targetObjectType=null,ViewType viewType=ViewType.Any ,PredefinedCategory category = PredefinedCategory.View) 
                => manager.RegisterViewAction(id, category.NewAction<SingleChoiceAction,ViewController>( targetObjectType.Configure(viewType)));

        public static IObservable<SingleChoiceAction> RegisterViewSingleChoiceAction(this ApplicationModulesManager manager, string id, 
             Action<SingleChoiceAction> configure ,PredefinedCategory category = PredefinedCategory.View) 
                => manager.RegisterViewAction(id, category.NewAction<SingleChoiceAction,ViewController>( configure));

        private static Func<(TController controller, string id), TAction> NewAction<TAction, TController>(
            this PredefinedCategory category, Action<TAction> configure) where TAction:ActionBase, new() where TController:Controller 
	        => _ => category.NewAction( configure, _);

        private static TAction NewAction<TAction, TController>(this PredefinedCategory category, Action<TAction> configure,
            (TController controller, string id) _) where TAction : ActionBase, new() where TController : Controller{
            var args = new object[]{_.controller,_.id,category};
            if (typeof(TAction) == typeof(ParametrizedAction)){
                args=args.AddItem(typeof(Type)).ToArray();
            }
            var action =  (TAction)Activator.CreateInstance(typeof(TAction),args);
            configure?.Invoke(action);
            if (action.Controller is ViewController viewController){
                viewController.TargetObjectType=action.TargetObjectType;
                viewController.TargetViewType=action.TargetViewType;
                viewController.TargetViewId=action.TargetViewId;
                viewController.TargetViewNesting=action.TargetViewNesting;
                viewController.TypeOfView = action.TypeOfView;
            }
            return action;
        }

        public static IObservable<ParametrizedAction> RegisterViewParametrizedAction(this ApplicationModulesManager manager, string id,Type valueType,
            Type targetObjectType=null,ViewType viewType=ViewType.Any,PredefinedCategory category=PredefinedCategory.View) 
	        => manager.RegisterViewAction(id, tuple => {
		        var parametrizedAction = category.NewAction(targetObjectType.Configure<ParametrizedAction>(viewType),tuple);
		        parametrizedAction.ValueType=valueType;
		        return parametrizedAction;
	        });

        public static IObservable<ParametrizedAction> RegisterViewParametrizedAction(this ApplicationModulesManager manager, string id,Type valueType,
            Action<ParametrizedAction> configure,PredefinedCategory category=PredefinedCategory.View) 
	        => manager.RegisterViewAction(id, tuple => {
                var parametrizedAction = category.NewAction(configure,tuple);
                parametrizedAction.ValueType=valueType;
                return parametrizedAction;
            });


        public static IObservable<SimpleAction> RegisterWindowSimpleAction(this ApplicationModulesManager manager, string id,
            PredefinedCategory category=PredefinedCategory.Tools,Action<SimpleAction> configure=null) 
	        => manager.RegisterWindowAction(id, category.NewAction<SimpleAction,WindowController>(configure));


        public static IObservable<SingleChoiceAction> RegisterWindowSingleChoiceAction(this ApplicationModulesManager manager, string id,
            PredefinedCategory category=PredefinedCategory.Tools,Action<SingleChoiceAction> configure=null) 
	        => manager.RegisterWindowAction(id, category.NewAction<SingleChoiceAction,WindowController>(configure));

        private static Action<T> Configure<T>(this Type targetObjectType, ViewType viewType) where T:ActionBase 
	        => action => {
                action.TargetObjectType = targetObjectType;
                action.TargetViewType = viewType;
            };

        private static Action<ActionBase> Configure(this Type targetObjectType, ViewType viewType) 
	        => targetObjectType.Configure<ActionBase>(viewType);

        public static IObservable<ParametrizedAction> RegisterWindowParametrizedAction(this ApplicationModulesManager manager, string id,Type valueType,
            PredefinedCategory category=PredefinedCategory.Tools,Action<ParametrizedAction> configure=null) 
	        => manager.RegisterWindowAction(id, _ => {
		        var parametrizedAction = category.NewAction(configure,_);
		        parametrizedAction.ValueType=valueType;
		        return parametrizedAction;
	        });

        public static IObservable<TAction> RegisterViewAction<TAction>(this ApplicationModulesManager applicationModulesManager, string id,
            Func<(ViewController controller, string id), TAction> actionBase) where TAction:ActionBase 
	        => applicationModulesManager.RegisterAction(id, actionBase);

        static IObservable<TAction> RegisterAction<TController,TAction>(this ApplicationModulesManager applicationModulesManager, string id,
            Func<(TController controller, string id), TAction> actionBase) where TController : Controller where TAction:ActionBase{
	        var type = ActionsModule.Assembly.GetType(ActionControllerName(id,GetBaseController<TController>()));
	        var controllerType = type ?? NewControllerType<TController>(id);
	        if (type == null){
		        ControllerCtorState
                    .AddOrUpdate(controllerType, type1 => (id, _ => actionBase(((TController) _.controller, _.id))),(_, tuple) => tuple);
            }

            // var registerAction = _actionsSubject.Select(a => a).OfType<TAction>().FirstAsync(_ => _.Id == id).Replay(1);
            // registerAction.Connect();
            // applicationModulesManager.Application().CreateController<TController>()
            var controller = (TController) Controller.Create(controllerType);
            applicationModulesManager.ControllersManager.RegisterController(controller);
            return ((IActionController) controller).WhenCloned
                .SelectMany(viewController => viewController.Actions).Cast<TAction>()
                .StartWith(controller.Actions.Cast<TAction>())
                // .Merge(controller.WhenClone().SelectMany(controller=>c.Actions))
                ;
        }

        public static IObservable<ActionBase> WhenActionAdded(this ActionList actionList)
            => Observable.FromEventPattern<EventHandler<ActionManipulationEventArgs>, ActionManipulationEventArgs>(
                h => actionList.ActionAdded += h, h => actionList.ActionAdded -= h, ImmediateScheduler.Instance)
                .Select(p => p.EventArgs.Action);

        static Subject<ActionBase> _actionsSubject=new();

        private static Type NewControllerType<T>(string id) where T:Controller{
            var baseController = GetBaseController<T>();
            var actionControllerName = ActionControllerName(id, baseController);
            try {
                return ActionsModule.GetType(actionControllerName)
                       ??ActionsModule.DefineType(actionControllerName, TypeAttributes.Public, baseController).CreateTypeInfo()?.AsType();
            }
            catch {
                return ActionsModule.GetType(actionControllerName);
            }
        }

        private static string ActionControllerName(string id, Type baseController) 
	        => $"{id}{baseController.Name}";

        private static Type GetBaseController<T>() where T : Controller 
	        => typeof(T) == typeof(ViewController) ? typeof(ActionViewController) : typeof(ActionWindowController);

        internal static void NewAction(this Controller controller){
	        var tuple = ControllerCtorState[controller.GetType()];
            var actionBase = tuple.actionBase((controller, tuple.id));
            actionBase.Caption = actionBase.Caption.Replace("_"," ").FirstCharacterToUpper().CompoundName();
        }
    }

    public abstract class ActionWindowController:WindowController,IActionController{
	    protected ActionWindowController() => this.NewAction();
        readonly Subject<Controller> _clonedSubject=new();
        public IObservable<Controller> WhenCloned => _clonedSubject.AsObservable();
        public override Controller Clone(IModelApplication modelApplication) {
            var controller = base.Clone(modelApplication);
            _clonedSubject.OnNext(this);
            return controller;
        }
    }

    public interface IActionController {
        IObservable<Controller> WhenCloned { get; }
    }

    public abstract class ActionViewController:ViewController, IActionController {
        protected ActionViewController() => this.NewAction();
        readonly Subject<ActionViewController> _clonedSubject=new();
        public IObservable<Controller> WhenCloned => _clonedSubject.AsObservable();

        public override Controller Clone(IModelApplication modelApplication) {
            var controller = base.Clone(modelApplication);
            _clonedSubject.OnNext((ActionViewController) controller);
            return controller;
        }
    }
}