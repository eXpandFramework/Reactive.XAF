using System;
using System.Reflection;
using System.Reflection.Emit;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Fasterflect;
using Ryder;


namespace Xpand.Source.Extensions.XAF.ApplicationModulesManager{
    public static class ApplicationModulesManagerExtensions{
        static ApplicationModulesManagerExtensions(){
            var dynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("ActionsAssembly"),AssemblyBuilderAccess.Run);
            ActionsModule = dynamicAssembly.DefineDynamicModule("ActionsModule");
        }

        private static ModuleBuilder ActionsModule{ get; }

        public static ActionBase RegisterMainWindowAction(this DevExpress.ExpressApp.ApplicationModulesManager applicationModulesManager, string id,
            Func<(WindowController controller, string id), ActionBase> actionBase){

            var registerWindowAction = applicationModulesManager.RegisterWindowAction(id, actionBase);
            ((WindowController) registerWindowAction.Controller).TargetWindowType=WindowType.Child;
            return registerWindowAction;
        }

        public static ActionBase RegisterChildWindowAction(this DevExpress.ExpressApp.ApplicationModulesManager applicationModulesManager, string id,
            Func<(WindowController controller, string id), ActionBase> actionBase){

            var registerWindowAction = applicationModulesManager.RegisterWindowAction(id, actionBase);
            ((WindowController) registerWindowAction.Controller).TargetWindowType=WindowType.Child;
            return registerWindowAction;
        }

        public static ActionBase RegisterWindowAction(this DevExpress.ExpressApp.ApplicationModulesManager applicationModulesManager, string id,
            Func<(WindowController controller, string id), ActionBase> actionBase){

            return applicationModulesManager.RegisterAction(id, actionBase);
        }

        public static ActionBase RegisterChildAction(this DevExpress.ExpressApp.ApplicationModulesManager applicationModulesManager, string id,
            Func<(WindowController controller, string id), ActionBase> actionBase){

            return applicationModulesManager.RegisterAction(id, actionBase);
        }

        public static ActionBase RegisterViewAction(this DevExpress.ExpressApp.ApplicationModulesManager applicationModulesManager, string id,
            Func<(ViewController controller, string id), ActionBase> actionBase){

            return applicationModulesManager.RegisterAction(id, actionBase);
        }
        

        public static ActionBase RegisterAction<TController>(this DevExpress.ExpressApp.ApplicationModulesManager applicationModulesManager,string id,Func<(TController controller,string id),ActionBase> actionBase) where TController:Controller{
            var type = ActionsModule.Assembly.GetType($"{id}{typeof(TController).Name}");
            var controllerType = type??NewControllerType<TController>(id);
            var controller = (TController)controllerType.CreateInstance();
            var action = actionBase((controller,id));
            controller.Actions.Add(action);
            if (type==null){
                Redirection.Observe(controllerType.Constructor(), context => {
                    var senderController = ((TController) context.Sender);
                    void AfterConstruction(object sender, EventArgs args){
                        var _ = ((TController) sender);
                        _.AfterConstruction -= AfterConstruction;
                        var controllerAction = actionBase((_, id));
                        _.Actions.Add(controllerAction);
                    }
                    senderController.AfterConstruction+= AfterConstruction;
                
                });
            }
            applicationModulesManager.ControllersManager.RegisterController(controller);
            return action;
        }


        private static Type NewControllerType<T>(string id) where T:Controller{
            var parent = typeof(T);
            return ActionsModule.DefineType($"{id}{parent.Name}", TypeAttributes.Public, parent).CreateType();
        }

        public static void RegisterAction<TViewController>(this DevExpress.ExpressApp.ApplicationModulesManager applicationModulesManager,
            params ActionBase[] actionBases) where TViewController : Controller{
            var controller = (TViewController) typeof(TViewController).CreateInstance();
            controller.Actions.AddRange(actionBases);
            applicationModulesManager.ControllersManager.RegisterController(controller);
        }
    }

}