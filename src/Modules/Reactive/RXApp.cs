using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text.RegularExpressions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Fasterflect;
using HarmonyLib;
using JetBrains.Annotations;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.AppDomainExtensions;
using Xpand.Extensions.XAF.ApplicationModulesManagerExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Attributes.Custom;
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

        static RxApp() {
            AppDomain.CurrentDomain.Patch(PatchXafApplication);
        }

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
        }

        private static MethodInfo GetMethodInfo(string methodName) 
            => typeof(RxApp).GetMethods(BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public).First(info => info.Name == methodName);

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
                .Merge(application.ShowPersistentObjectsInNonPersistentView())
            ))
            .Merge(manager.SetupPropertyEditorParentView())
            .Merge(manager.MergedExtraEmbeddedModels())
            .Merge(manager.InvisibleInAllViewsAttribute())
            .Merge(manager.VisibleInAllViewsAttribute())
            .Merge(manager.CustomAttributes())
            .Merge(manager.XpoAttributes())
        ;


        static IObservable<Unit> CustomAttributes(this ApplicationModulesManager manager) 
            => manager.WhenCustomizeTypesInfo()
                .SelectMany(t => t.e.TypesInfo.PersistentTypes
                    .SelectMany(info => info.Members.SelectMany(memberInfo => memberInfo.FindAttributes<Attribute>().OfType<ICustomAttribute>().ToArray()
                        .Select(attribute => {
                            for (int index = 0; index < attribute.Name.Split(';').Length; index++) {
                                string s = attribute.Name.Split(';')[index];
                                var theValue = attribute.Value.Split(';')[index];
                                memberInfo.AddAttribute(new ModelDefaultAttribute(s, theValue));
                            }
                            return attribute;
                        }))))
                .ToUnit();

        static IObservable<Unit> VisibleInAllViewsAttribute(this ApplicationModulesManager manager)
            => manager.WhenCustomizeTypesInfo()
                .SelectMany(t => t.e.TypesInfo.PersistentTypes
                    .SelectMany(info => info.Members.Where(memberInfo => memberInfo.FindAttributes<VisibleInAllViewsAttribute>().Any())))
                .SelectMany(info => new Attribute[] {new VisibleInDetailViewAttribute(true), new VisibleInListViewAttribute(true), new VisibleInLookupListViewAttribute(true)}
                    .ToObservable(ImmediateScheduler.Instance)
                    .Do(info.AddAttribute))
                .ToUnit();

        static IObservable<Unit> InvisibleInAllViewsAttribute(this ApplicationModulesManager manager)
            => manager.WhenCustomizeTypesInfo()
                .SelectMany(t => t.e.TypesInfo.PersistentTypes
                    .SelectMany(info => info.Members.Where(memberInfo => memberInfo.FindAttributes<InvisibleInAllViewsAttribute>().Any())))
                .SelectMany(info => new Attribute[] {
                        new VisibleInDetailViewAttribute(false), new VisibleInListViewAttribute(false),
                        new VisibleInLookupListViewAttribute(false)
                    }
                    .ToObservable(ImmediateScheduler.Instance)
                    .Do(info.AddAttribute))
                .ToUnit();

        static IObservable<Unit> PatchAuthentication(this XafApplication application) 
            => application.WhenSetupComplete()
                .Do(_ => {
                    AppDomain.CurrentDomain.Patch(harmony => {
                        if (application.Security.IsInstanceOf("DevExpress.ExpressApp.Security.SecurityStrategyBase")){
                            var methodInfo = application.Security?.GetPropertyValue("Authentication")?.GetType().Methods("Authenticate")
                                .Last(info =>info.DeclaringType!=null&& !info.DeclaringType.IsAbstract);
                            if (methodInfo != null){
                                harmony.Patch(methodInfo, new HarmonyMethod(GetMethodInfo(nameof(Authenticate))));	
                            }	
                        }
                    });
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

        private static IObservable<Unit> XpoAttributes(this ApplicationModulesManager manager)
            => manager.WhenCustomizeTypesInfo()
                .SelectMany(t => new[]{"SingleObjectAttribute","PropertyConcatAttribute"})
                .SelectMany(attributeName => {
                    var lastObjectAttributeType = AppDomain.CurrentDomain.GetAssemblyType($"Xpand.Extensions.XAF.Xpo.{attributeName}");
                    return lastObjectAttributeType != null ? (IEnumerable<IMemberInfo>) lastObjectAttributeType
                        .Method("Configure", Flags.StaticAnyVisibility).Call(null) : Enumerable.Empty<IMemberInfo>();
                } )
                .ToUnit();

        private static IObservable<Unit> MergedExtraEmbeddedModels(this ApplicationModulesManager manager) 
            => manager.WhereApplication().ToObservable()
                .SelectMany(application => application.WhenCreateCustomModelDifferenceStore()
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