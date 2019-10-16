using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Fasterflect;
using HarmonyLib;

namespace Xpand.Extensions.XAF.ApplicationModulesManager{
    public static partial class ApplicationModulesManagerExtensions{
        public static object Locker=new object();
        private static readonly Harmony Harmony;
        static readonly ConcurrentDictionary<Type,(string id,Func<(Controller controller, string id), ActionBase> actionBase)> ControllerCtorState=new ConcurrentDictionary<Type, (string id, Func<(Controller controller, string id), ActionBase> actionBase)>();
        static ApplicationModulesManagerExtensions(){
            Harmony = new Harmony(typeof(ApplicationModulesManagerExtensions).FullName);
            var dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ActionsAssembly"),AssemblyBuilderAccess.Run);
            ActionsModule = dynamicAssembly.DefineDynamicModule("ActionsModule");
        }

        private static ModuleBuilder ActionsModule{ get; }

        public static ActionBase RegisterMainWindowAction(this DevExpress.ExpressApp.ApplicationModulesManager applicationModulesManager, string id,
            Func<(Controller controller, string id), ActionBase> actionBase){

            var registerWindowAction = applicationModulesManager.RegisterWindowAction(id, actionBase);
            ((WindowController) registerWindowAction.Controller).TargetWindowType=WindowType.Child;
            return registerWindowAction;
        }

        public static ActionBase RegisterChildWindowAction(this DevExpress.ExpressApp.ApplicationModulesManager applicationModulesManager, string id,
            Func<(Controller controller, string id), ActionBase> actionBase){

            var registerWindowAction = applicationModulesManager.RegisterWindowAction(id, actionBase);
            ((WindowController) registerWindowAction.Controller).TargetWindowType=WindowType.Child;
            return registerWindowAction;
        }

        public static ActionBase RegisterWindowAction(this DevExpress.ExpressApp.ApplicationModulesManager applicationModulesManager, string id,
            Func<(Controller controller, string id), ActionBase> actionBase){

            return applicationModulesManager.RegisterAction<WindowController>(id, actionBase);
        }

        public static ActionBase RegisterChildAction(this DevExpress.ExpressApp.ApplicationModulesManager applicationModulesManager, string id,
            Func<(Controller controller, string id), ActionBase> actionBase){

            return applicationModulesManager.RegisterAction<WindowController>(id, actionBase);
        }

        public static ActionBase RegisterViewAction(this DevExpress.ExpressApp.ApplicationModulesManager applicationModulesManager, string id,
            Func<(Controller controller, string id), ActionBase> actionBase){

            return applicationModulesManager.RegisterAction<ViewController>(id, actionBase);
        }
        
        
        public static ActionBase RegisterAction<TController>(this DevExpress.ExpressApp.ApplicationModulesManager applicationModulesManager, string id,
            Func<(Controller controller, string id), ActionBase> actionBase) where TController : Controller{
            lock (ActionsModule){
                var type = ActionsModule.Assembly.GetType($"{id}{typeof(TController).Name}");
                var controllerType = type??NewControllerType<TController>(id);
                var controller = (TController)controllerType.CreateInstance();
                var action = actionBase((controller,id));
                controller.Actions.Add(action);
                if (type==null){
                    ControllerCtorState.TryAdd(controllerType, (id,actionBase));
                    var controllerCtorPatch = typeof(ApplicationModulesManagerExtensions).Method(nameof(ControllerCtorPatch),Flags.StaticAnyVisibility);
                    Harmony.Patch(controllerType.Constructor(),finalizer: new HarmonyMethod(controllerCtorPatch));
                }
                applicationModulesManager.ControllersManager.RegisterController(controller);
                return action;
            }
        }

        // ReSharper disable once InconsistentNaming
        private static void ControllerCtorPatch(Controller __instance){
            void AfterConstruction(object sender, EventArgs args){
                var _ = (Controller) sender;
                _.AfterConstruction -= AfterConstruction;
                var tuple = ControllerCtorState[_.GetType()];
                var controllerAction = tuple.actionBase((_, tuple.id));
                _.Actions.Add(controllerAction);
            }
            __instance.AfterConstruction+= AfterConstruction;
        }

        private static Type NewControllerType<T>(string id) where T:Controller{
            var parent = typeof(T);
            var controllerType = ActionsModule.DefineType($"{id}{parent.Name}", TypeAttributes.Public, parent).CreateType();
            return controllerType;
        }

        public static void RegisterAction<TViewController>(this DevExpress.ExpressApp.ApplicationModulesManager applicationModulesManager,
            params ActionBase[] actionBases) where TViewController : Controller{
            var controller = (TViewController) typeof(TViewController).CreateInstance();
            controller.Actions.AddRange(actionBases);
            applicationModulesManager.ControllersManager.RegisterController(controller);
        }
    }

}