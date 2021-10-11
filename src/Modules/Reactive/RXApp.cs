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
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.AppDomainExtensions;
using Xpand.Extensions.XAF.ApplicationModulesManagerExtensions;
using Xpand.Extensions.XAF.ModuleExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Security;

namespace Xpand.XAF.Modules.Reactive{
	public static class RxApp{
        
        static readonly Subject<ApplicationModulesManager> ApplicationModulesManagerSubject=new();
        static readonly Subject<Frame> FramesSubject=new();

        static RxApp() => AppDomain.CurrentDomain.Patch(PatchXafApplication);

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static void CreateModuleManager(ApplicationModulesManager __result) => ApplicationModulesManagerSubject.OnNext(__result);

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static void Initialize(Frame __instance) => FramesSubject.OnNext(__instance);

        private static void PatchXafApplication(Harmony harmony){
            var frameInitialize = typeof(Frame).Method("Initialize");
            var createFrameMethodPatch = GetMethodInfo(nameof(Initialize));
            harmony.Patch(frameInitialize,  postfix:new HarmonyMethod(createFrameMethodPatch));
            var createModuleManager = typeof(XafApplication).Method(nameof(CreateModuleManager));
            harmony.Patch(createModuleManager, finalizer: new HarmonyMethod(GetMethodInfo(nameof(CreateModuleManager))));
            harmony.Patch(typeof(XafApplication).Method(nameof(XafApplication.Exit)),
                new HarmonyMethod(typeof(XafApplicationRxExtensions), nameof(XafApplicationRxExtensions.Exit)));
        }

        private static MethodInfo GetMethodInfo(string methodName) 
            => typeof(RxApp).GetMethods(BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public).First(info => info.Name == methodName);

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager)
            => manager.Attributes()
                .Merge(manager.AddNonSecuredTypes())
                .Merge(manager.MergedExtraEmbeddedModels())
                .Merge(manager.ConnectObjectString())
                .Merge(manager.WhenApplication(application =>application.WhenNonPersistentPropertyCollectionSource()
                    .Merge(application.PatchAuthentication())
                    .Merge(application.PatchObjectSpaceProvider())
                    .Merge(application.ShowPersistentObjectsInNonPersistentView())))
                .Merge(manager.SetupPropertyEditorParentView());



        private static IObservable<Unit> MergedExtraEmbeddedModels(this ApplicationModulesManager manager) 
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

        internal static IObservable<Window> PopupWindows => Frames.When(TemplateContext.PopupWindow).OfType<Window>();

        internal static IObservable<Frame> Frames{ get; } = FramesSubject.DistinctUntilChanged();

        
    }

}