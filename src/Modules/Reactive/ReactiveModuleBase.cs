using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive{
    public abstract partial class ReactiveModuleBase:ModuleBase{
        internal readonly ReplaySubject<ReactiveModuleBase> SetupCompletedSubject=new ReplaySubject<ReactiveModuleBase>(1);
        static readonly Subject<ApplicationModulesManager> SettingUpSubject=new Subject<ApplicationModulesManager>();

        public static void Unload(params Type[] modules) =>
	        SettingUpSubject.Do(_ => {
			        foreach (var module in _.Modules.Where(m => m.RequiredModuleTypes.Any(modules.Contains))){
				        foreach (var type in modules){
					        module.RequiredModuleTypes.Remove(type);
				        }
			        }
			        foreach (var m in modules){
				        var module = _.Modules.FindModule(m);
				        _.Modules.Remove(module);
				        module.Dispose();
			        }
		        })
		        .FirstAsync().Subscribe();

        public IObservable<ReactiveModuleBase> SetupCompleted => Observable.Defer(() => SetupCompletedSubject.Select(module => module)).TraceRX();


        private static bool SetupModules(ApplicationModulesManager applicationModulesManager){
            SettingUpSubject.OnNext(applicationModulesManager);
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
