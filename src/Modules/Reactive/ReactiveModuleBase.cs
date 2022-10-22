using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using Fasterflect;
using HarmonyLib;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.XAF.AppDomainExtensions;
using Xpand.Extensions.XAF.Harmony;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive{
    public abstract class ReactiveModuleBase:ModuleBase{
        internal readonly ReplaySubject<ReactiveModuleBase> SetupCompletedSubject=new(1);
        static readonly Subject<ApplicationModulesManager> SettingUpSubject=new();
        static ReactiveModuleBase(){
            // AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
            // new HarmonyMethod(typeof(ReactiveModule).Method(nameof(SetupModulesPatch),Flags.StaticAnyVisibility))
            //     .PreFix(typeof(ApplicationModulesManager).Method("SetupModules"),true);
            // if (DesignerOnlyCalculator.IsRunTime) {
            //     AppDomain.CurrentDomain.AddModelReference("netstandard", typeof(FontStyle?).Assembly.GetName().Name);
            // }
        }

        private static Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args){
            var name = args.Name;
            var comma = name.IndexOf(",", StringComparison.Ordinal);
            if (comma > -1){
                name = args.Name.Substring(0, comma);
            }
        
            try {
                var path = $@"{AppDomain.CurrentDomain.ApplicationPath()}\{name}.dll";
                return File.Exists(path) ? Assembly.LoadFile(path) : null;
            }
            catch (Exception e){
                Tracing.Tracer.LogError(e);
                return null;
            }
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static bool SetupModulesPatch(ApplicationModulesManager __instance){
            Tracing.Tracer.LogText("SetupModules");
            return SetupModules(__instance);

        }
        public static Type[] UnloadedModules { get; private set; }
        

        
        public static IDisposable Unload(params Type[] modules) {
            UnloadedModules=modules;
            return SettingUpSubject.Do(_ => {
                    foreach (var module in _.Modules.Where(m => m.RequiredModuleTypes.Any(modules.Contains))) {
                        foreach (var type in modules) {
                            module.RequiredModuleTypes.Remove(type);
                        }
                    }

                    foreach (var m in modules) {
                        var module = _.Modules.FindModule(m);
                        _.Modules.Remove(module);
                        module.Dispose();
                    }
                })
                .FirstAsync().Subscribe();
        }

        
        public IObservable<ReactiveModuleBase> SetupCompleted => Observable.Defer(() => SetupCompletedSubject.Select(module => module)).TraceRX();


        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
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
