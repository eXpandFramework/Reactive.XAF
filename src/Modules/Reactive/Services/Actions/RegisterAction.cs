using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Reflection.Emit;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Fasterflect;
using HarmonyLib;
using JetBrains.Annotations;

namespace Xpand.XAF.Modules.Reactive.Services.Actions{
    [PublicAPI]
    public static partial class ActionsService{
        public static object Locker=new object();
        private static readonly Harmony Harmony;
        static readonly ConcurrentDictionary<Type,(string id,Func<(Controller controller, string id), ActionBase> actionBase)> ControllerCtorState=new ConcurrentDictionary<Type, (string id, Func<(Controller controller, string id), ActionBase> actionBase)>();
        static ActionsService(){
            Harmony = new Harmony(typeof(ActionsService).FullName);
            var dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ActionsAssembly"),AssemblyBuilderAccess.Run);
            ActionsModule = dynamicAssembly.DefineDynamicModule("ActionsModule");
        }

        private static ModuleBuilder ActionsModule{ get; }

        public static IObservable<TAction> RegisterMainWindowAction<TAction>(this ApplicationModulesManager applicationModulesManager, string id,
            Func<(WindowController controller, string id), TAction> actionBase) where  TAction:ActionBase{
            return applicationModulesManager.RegisterWindowAction(id, actionBase).Do(_ => ((WindowController) _.Controller).TargetWindowType = WindowType.Child);
        }

        public static IObservable<TAction> RegisterChildWindowAction<TAction>(this ApplicationModulesManager applicationModulesManager, string id,
            Func<(WindowController controller, string id), TAction> actionBase) where  TAction:ActionBase{

            return applicationModulesManager.RegisterWindowAction(id, actionBase)
                .Do(_ => ((WindowController) _.Controller).TargetWindowType=WindowType.Child);
        }

        public static IObservable<TAction> RegisterWindowAction<TAction>(this ApplicationModulesManager applicationModulesManager, string id,
            Func<(WindowController controller, string id), TAction> actionBase) where TAction:ActionBase{

            return applicationModulesManager.RegisterAction(id, actionBase);
        }

        public static IObservable<PopupWindowShowAction> RegisterViewPopupWindowShowAction(this ApplicationModulesManager manager, string id,PredefinedCategory category=PredefinedCategory.View){
            return manager.RegisterViewAction(id, _ => new PopupWindowShowAction(_.controller,id, category));
        }
        public static IObservable<PopupWindowShowAction> RegisterWindowPopupWindowShowAction(this ApplicationModulesManager manager, string id,PredefinedCategory category=PredefinedCategory.View){
            return manager.RegisterWindowAction(id, _ => new PopupWindowShowAction(_.controller,id, category));
        }

        public static IObservable<SimpleAction> RegisterViewSimpleAction(this ApplicationModulesManager manager, string id,PredefinedCategory category=PredefinedCategory.View){
            return manager.RegisterViewAction(id, _ => new SimpleAction(_.controller, _.id, category));
        }
        
        public static IObservable<SingleChoiceAction> RegisterViewSingleChoiceAction(this ApplicationModulesManager manager, string id,PredefinedCategory category=PredefinedCategory.View){
            return manager.RegisterViewAction(id, _ => new SingleChoiceAction(_.controller, _.id, category));
        }

        public static IObservable<ParametrizedAction> RegisterViewParametrizedAction(this ApplicationModulesManager manager, string id,Type valueType,PredefinedCategory category=PredefinedCategory.View){
            return manager.RegisterViewAction(id, _ => new ParametrizedAction(_.controller, _.id, category, valueType));
        }

        public static IObservable<SimpleAction> RegisterWindowSimpleAction(this ApplicationModulesManager manager, string id,PredefinedCategory category=PredefinedCategory.Tools){
            return manager.RegisterWindowAction(id, _ => new SimpleAction(_.controller, _.id, category));
        }
        
        public static IObservable<SingleChoiceAction> RegisterWindowSingleChoiceAction(this ApplicationModulesManager manager, string id,PredefinedCategory category=PredefinedCategory.Tools){
            return manager.RegisterWindowAction(id, _ => new SingleChoiceAction(_.controller, _.id, category));
        }

        public static IObservable<ParametrizedAction> RegisterWindowParametrizedAction(this ApplicationModulesManager manager, string id,Type valueType,PredefinedCategory category=PredefinedCategory.Tools){
            return manager.RegisterWindowAction(id, _ => new ParametrizedAction(_.controller, _.id, category, valueType));
        }

        public static IObservable<TAction> RegisterViewAction<TAction>(this ApplicationModulesManager applicationModulesManager, string id,
            Func<(ViewController controller, string id), TAction> actionBase) where TAction:ActionBase{
            return applicationModulesManager.RegisterAction(id, actionBase);
        }
        
        public static IObservable<TAction> RegisterAction<TController,TAction>(this ApplicationModulesManager applicationModulesManager, string id,
            Func<(TController controller, string id), TAction> actionBase) where TController : Controller where TAction:ActionBase{
            lock (ActionsModule){
                return Observable.Create<TAction>(observer => {
                    var type = ActionsModule.Assembly.GetType($"{id}{typeof(TController).Name}");
                    var controllerType = type ?? NewControllerType<TController>(id);
                    var controller = (TController) controllerType.CreateInstance();
                    var action = actionBase((controller, id));
                    controller.Actions.Add(action);
                    observer.OnNext(action);
                    if (type == null){
                        ControllerCtorState.TryAdd(controllerType,
                            (id, _ => actionBase(((TController) _.controller, _.id))));
                        var controllerCtorPatch =
                            typeof(ActionsService).Method(nameof(ControllerCtorPatch), Flags.StaticAnyVisibility);
                        Harmony.Patch(controllerType.Constructor(), finalizer: new HarmonyMethod(controllerCtorPatch));
                    }

                    applicationModulesManager.ControllersManager.RegisterController(controller);
                    return action;
                }).Merge(_actionsSubject.OfType<TAction>().Where(_ => _.Id==id));
            }
        }

        static Subject<ActionBase> _actionsSubject=new Subject<ActionBase>();
        
        
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static void ControllerCtorPatch(Controller __instance){
            void AfterConstruction(object sender, EventArgs args){
                var _ = (Controller) sender;
                _.AfterConstruction -= AfterConstruction;
                var tuple = ControllerCtorState[_.GetType()];
                var controllerAction = tuple.actionBase((_, tuple.id));
                _.Actions.Add(controllerAction);
                _actionsSubject.OnNext(controllerAction);
            }
            __instance.AfterConstruction+= AfterConstruction;
        }

        private static Type NewControllerType<T>(string id) where T:Controller{
            var parent = typeof(T);
            return ActionsModule.DefineType($"{id}{parent.Name}", TypeAttributes.Public, parent).CreateType();
        }
    }

}