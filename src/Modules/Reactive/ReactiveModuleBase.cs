using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive{
    public abstract partial class ReactiveModuleBase:ModuleBase{
        internal ReplaySubject<ReactiveModuleBase> SetupCompletedSubject=new ReplaySubject<ReactiveModuleBase>(1);
        public void Unload(){
            Application.Modules.Remove(this);
            Dispose();
        }

        public IObservable<ReactiveModuleBase> SetupCompleted => Observable.Defer(() => SetupCompletedSubject.Select(module => module)).TraceRX();


        private static bool SetupModules(ApplicationModulesManager applicationModulesManager){
            foreach(var module in applicationModulesManager.Modules) {
                try {
                    module.Setup(applicationModulesManager);
                    if (module is ReactiveModuleBase reactiveModuleBase){
                        reactiveModuleBase.SetupCompletedSubject.OnNext(reactiveModuleBase);
                        reactiveModuleBase.SetupCompletedSubject.OnCompleted();
                    }
                }
                catch(Exception e) {
                    throw new InvalidOperationException($"Exception occurs while initializing the '{module.GetType().FullName}' module: {e.Message}", e);
                }
            }
            foreach(var controller in ((ReadOnlyCollection<Controller>) applicationModulesManager.ControllersManager.GetPropertyValue("Controllers"))) {
                if(controller is ISupportSetup supportSetupItem) {
                    try {
                        supportSetupItem.Setup(applicationModulesManager);
                    }
                    catch(Exception e) {
                        throw new InvalidOperationException($"Exception occurs while initializing the '{controller.GetType().FullName}' controller: {e.Message}", e);
                    }
                }
            }            
            return false;
        }
    }
}
