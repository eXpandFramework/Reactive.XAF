using System;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text.RegularExpressions;
using DevExpress.ExpressApp;
using Fasterflect;
using HarmonyLib;
using JetBrains.Annotations;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ApplicationModulesManagerExtensions;
using Xpand.Extensions.XAF.ModuleExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Security;

namespace Xpand.XAF.Modules.Reactive{
	public static class RxApp{
        internal static readonly ISubject<(object authentication, GenericEventArgs<object> args)>
            AuthenticateSubject = Subject.Synchronize(new Subject<(object authentication, GenericEventArgs<object> args)>());
        static readonly Subject<ApplicationModulesManager> ApplicationModulesManagerSubject=new Subject<ApplicationModulesManager>();
        static readonly Subject<Frame> FramesSubject=new Subject<Frame>();
        static readonly Subject<(object theObject,IObjectSpace objectSpace)> NewObjectsSubject=new Subject<(object theObject, IObjectSpace objectSpace)>();
        static readonly Subject<Window> PopupWindowsSubject=new Subject<Window>();
        internal static Harmony Harmony;

        static RxApp(){
            Harmony = new Harmony(typeof(RxApp).Namespace);
	        PatchXafApplication(Harmony);
	        var methodInfo = typeof(BaseObjectSpace).Methods(Flags.AnyVisibility|Flags.Instance,nameof(BaseObjectSpace.CreateObject)).First(info => !info.IsGenericMethod);
	        var method = GetMethodInfo(nameof(CreateObject));
	        Harmony.Patch(methodInfo,postfix: new HarmonyMethod(method));
        }
        
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static void CreateObject(IObjectSpace __instance,object __result){
            NewObjectsSubject.OnNext(( __result,__instance));
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static void CreateModuleManager(ApplicationModulesManager __result){
            ApplicationModulesManagerSubject.OnNext(__result);
        }
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static void CreateFrame(Frame __result){
            FramesSubject.OnNext(__result);
        }
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static void CreateWindow(Window __result){
            FramesSubject.OnNext(__result);
        }
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static void CreatePopupWindow(Window __result){
            FramesSubject.OnNext(__result);
            PopupWindowsSubject.OnNext(__result);
        }
        
        public static IObservable<(object theObject,IObjectSpace objectSpace)> NewObjects 
            => NewObjectsSubject.AsObservable();

        private static void PatchXafApplication(Harmony harmony){
            var xafApplicationMethods = typeof(XafApplication).Methods();
            var createFrameMethodPatch = GetMethodInfo(nameof(CreateFrame));
            var frameMethods = new[]{
                xafApplicationMethods.First(info => info.Name == nameof(XafApplication.CreateNestedFrame)),
                xafApplicationMethods.First(info => info.Name == nameof(XafApplication.CreateFrame))
            };
            foreach (var frameMethod in frameMethods){
                harmony.Patch(frameMethod, finalizer: new HarmonyMethod(createFrameMethodPatch));
            }

            var createWindows = xafApplicationMethods.Where(info =>
                info.Name == nameof(XafApplication.CreateWindow) );
            foreach (var createWindow in createWindows){
                harmony.Patch(createWindow, finalizer: new HarmonyMethod(GetMethodInfo(nameof(CreateWindow))));    
            }
            
            
            var createPopupWindow = xafApplicationMethods.First(info => info.Name == nameof(CreatePopupWindow)&&info.Parameters().Count==5);
            harmony.Patch(createPopupWindow, finalizer: new HarmonyMethod(GetMethodInfo(nameof(CreatePopupWindow))));
            
            var createModuleManager = xafApplicationMethods.First(info => info.Name == nameof(CreateModuleManager));
            harmony.Patch(createModuleManager, finalizer: new HarmonyMethod(GetMethodInfo(nameof(CreateModuleManager))));
        }

        private static MethodInfo GetMethodInfo(string methodName) 
            => typeof(RxApp).GetMethods(BindingFlags.Static|BindingFlags.NonPublic).First(info => info.Name == methodName);

        private static IObservable<Unit> AddNonSecuredTypes(this ApplicationModulesManager applicationModulesManager) 
            => applicationModulesManager.WhenCustomizeTypesInfo()
                .Select(_ =>_.e.TypesInfo.PersistentTypes.Where(info => info.Attributes.OfType<NonSecuredTypeAttrbute>().Any())
                    .Select(info => info.Type))
                .Do(infos => {
                    var xafApplication = applicationModulesManager.Application();
                    xafApplication?.AddNonSecuredType(infos.ToArray());
                })
                .ToUnit();

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.AddNonSecuredTypes()
                .Merge(manager.WhenApplication(application => application.WhenNonPersistentPropertyCollectionSource()
                .Merge(application.PatchAuthentication())
                .Merge(application.PatchObjectSpaceProvider())
                .Merge(application.ShowPersistentObjectsInNonPersistentView())
            ))
            .Merge(manager.SetupPropertyEditorParentView())
            .Merge(manager.MergedExtraEmbededModels());


        static IObservable<Unit> PatchAuthentication(this XafApplication application) 
            => application.WhenSetupComplete()
                .Do(_ => {
                    var harmony = new Harmony(nameof(PatchAuthentication));
                    if (application.Security.IsInstanceOf("DevExpress.ExpressApp.Security.SecurityStrategyBase")){
                        var methodInfo = ( application.Security)?.GetPropertyValue("Authentication")?.GetType().Methods("Authenticate")
                            .Last(info =>info.DeclaringType!=null&& !info.DeclaringType.IsAbstract);
                        if (methodInfo != null){
                            harmony.Patch(methodInfo, new HarmonyMethod(GetMethodInfo(nameof(Authenticate))));	
                        }	
                    }
                })
                .ToUnit();

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool Authenticate(ref object __result,object __instance,IObjectSpace objectSpace) {
            var args = new GenericEventArgs<object>();
            AuthenticateSubject.OnNext((__instance, args));
            if (args.Instance != null){
                __result = objectSpace.GetObjectByKey((Type) __instance.GetPropertyValue("UserType"), args.Instance);
                return false;
            }
            return true;
        }
        
        private static IObservable<Unit> MergedExtraEmbededModels(this ApplicationModulesManager manager) 
            => manager.WhereApplication().ToObservable()
                .SelectMany(application => application.WhenCreateCustomUserModelDifferenceStore()
                    .Do(_ => {
                        var models = _.application.Modules.SelectMany(m => m.EmbeddedModels().Select(tuple => (id: $"{m.Name},{tuple.id}", tuple.model)))
                            .Where(tuple => {
                                var pattern = ConfigurationManager.AppSettings["EmbeddedModels"]??@"(\.MDO)|(\.RDO)";
                                return !Regex.IsMatch(tuple.id, pattern, RegexOptions.Singleline);
                            })
                            .ToArray();
                        foreach (var model in models){
                            _.e.AddExtraDiffStore(model.id, new StringModelStore(model.model));
                        }

                        if (models.Any()){
                            _.e.AddExtraDiffStore("After Setup", new ModelStoreBase.EmptyModelStore());
                        }
                    })).ToUnit();

        private static IObservable<Unit> SetupPropertyEditorParentView(this ApplicationModulesManager applicationModulesManager) 
            => applicationModulesManager.WhereApplication().ToObservable().SelectMany(_ => _.SetupPropertyEditorParentView());

        [PublicAPI]
        public static IObservable<Unit> UpdateMainWindowStatus<T>(IObservable<T> messages,TimeSpan period=default){
            if (period==default)
                period=TimeSpan.FromSeconds(5);
            return WindowTemplateService.UpdateStatus(period, messages);
        }

        internal static IObservable<ApplicationModulesManager> ApplicationModulesManager => ApplicationModulesManagerSubject.AsObservable();

        internal static IObservable<Window> PopupWindows => PopupWindowsSubject;

        internal static IObservable<Frame> Frames{ get; } = FramesSubject.DistinctUntilChanged()
	        .Merge(PopupWindows);

        
    }

}