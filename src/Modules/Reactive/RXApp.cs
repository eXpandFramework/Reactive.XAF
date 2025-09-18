using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Fasterflect;
using HarmonyLib;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.ApplicationModulesManagerExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Harmony;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.ModuleExtensions;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Security;

namespace Xpand.XAF.Modules.Reactive{
    public static class RxApp{
        static readonly ReplaySubject<SynchronizationContext> ContextSubject = new();
        static readonly Subject<ApplicationModulesManager> ApplicationModulesManagerSubject=new();
        static readonly Subject<(List<Controller> __result, Type baseType, IModelApplication modelApplication, View view)> WhenControllerCreatedSubject=new();
        

        static RxApp() => PatchXafApplication();

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static void CreateModuleManager(ApplicationModulesManager __result) => ApplicationModulesManagerSubject.OnNext(__result);

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static void CreateControllers(Type baseType,
            IModelApplication modelApplication,
            View view,List<Controller> __result) {
            WhenControllerCreatedSubject.OnNext(( __result, baseType, modelApplication,
                view));
        }

        public static IObservable<List<Controller>> ToControllers(
            this IObservable<(XafApplication Application, List<Controller> controllers, Type baseType, IModelApplication
                modelApplication, View view)> source)
            => source.Select(t => t.controllers);


        public static IObservable<(List<Controller> controllers, Type baseType, IModelApplication modelApplication, View view)> ControllerCreated 
            => WhenControllerCreatedSubject.AsObservable();

        public static IObservable<T> When<T>(
            this IObservable<(List<Controller> controllers, Type baseType, IModelApplication modelApplication, View view)> source) 
            => source.SelectMany(t => t.controllers.Where(controller => controller is T)).Cast<T>();


        private static void PatchXafApplication(){
            var xafApplicationType = typeof(XafApplication);
            
            new HarmonyMethod(GetMethodInfo(nameof(CreateModuleManager)))
                .Finalize(xafApplicationType.Method(nameof(CreateModuleManager)),true);
            
            
            if (DesignerOnlyCalculator.IsRunTime) {
                new HarmonyMethod(GetMethodInfo(nameof(CreateControllers)))
                    .Finalize(typeof(ControllersManager).Method(nameof(ControllersManager.CreateControllers), [typeof(Type),typeof(IModelApplication),typeof(View)
                    ]),true);
            }
            
        }

        private static MethodInfo GetMethodInfo(string methodName) 
            => typeof(RxApp).GetMethods(BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public).First(info => info.Name == methodName);

        internal static IObservable<Unit> NonPersistentChangesEnabledAttribute(this XafApplication application) 
            => application.WhenObjectViewCreated().Where(view => view.ObjectTypeInfo.FindAttributes<NonPersistentChangesEnabledAttribute>().Any())
                .Do(view => view.ObjectSpace.NonPersistentChangesEnabled = true).ToUnit()
                .PushStackFrame();

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager)
        => manager.Attributes()
                .Merge(manager.AddNonSecuredTypes())
                .Merge(manager.MergedExtraEmbeddedModels())
                .Merge(manager.ConnectObjectString())
                .Merge(manager.WhenApplication(application =>application.Connect()
                    .MergeToUnit(application.WhenSynchronizationContext().Do(context => ContextSubject.OnNext(context)))
                    .Merge(manager.SetupPropertyEditorParentView()))
        )
        .PushStackFrame();

        public static IObservable<Unit> ThrowOnContext(this Exception exception) {
            return ContextSubject.SelectMany(_ => Observable.Throw<Unit>(exception)).ToUnit();
        }

        private static IObservable<Unit> Connect(this XafApplication application) {
            if (!AppDomain.CurrentDomain.UseNetFramework()) {
                new HarmonyMethod(typeof(XafApplicationRxExtensions), nameof(XafApplicationRxExtensions.Exit))
                    .PreFix(typeof(XafApplication).Method(nameof(XafApplication.Exit)),true);
            }
            
            return application.PatchAuthentication()
                    .Merge(application.WhenNonPersistentPropertyCollectionSource())
                    .Merge(application.PatchObjectSpaceProvider())
                    .Merge(application.NonPersistentChangesEnabledAttribute())
                    .Merge(application.PopulateAdditionalObjectSpaces())
                    .Merge(application.ReloadWhenChanged())
                    .Merge(application.FireChanged())
                    .Merge(application.ShowMessages())
                    .Merge(application.ExplicitModificationAttribute())
                    .Merge(application.ShowInstanceDetailView())
                    .PushStackFrame();
        }
        
        
        private static IObservable<Unit> ExplicitModificationAttribute(this XafApplication application)
            => application.WhenSetupComplete()
                .SelectMany(_ => application.WhenFrame(application.TypesInfo.PersistentTypes.Attributed<ExplicitModificationAttribute>().Types(true).Select(info => info.Type).ToArray())
                    .WhenFrame(ViewType.DetailView)
                .SelectManyItemResilient(frame => {
                    var propertyName = frame.View.ObjectTypeInfo.Attributed<ExplicitModificationAttribute>().Select(t => t.attribute.PropertyName).First();
                    var currentObject = ((IObjectSpaceLink)frame.View.CurrentObject);
                    if (frame.View.CurrentObject == null) return Observable.Empty<Unit>();
                    var memberInfoValueDictionary = currentObject.MemberInfoValueDictionary();
                    var memberInfo = frame.View.ObjectTypeInfo.FindMember(propertyName);
                    var properties = $"{memberInfo.GetValue(currentObject)}".Split(',').WhereNotEmpty().ToHashSet();
                    return frame.View.ObjectSpace.WhenModifiedObjects(frame.View.ObjectTypeInfo.Type, properties.Where(s => s!=propertyName).ToArray())
                        .SelectMany(_ => currentObject.CompareTypeInfoValue(memberInfoValueDictionary)
                            .Keys.ToNowObservable().Do(s => properties.Add(s)))
                        .MergeToUnit(frame.View.ObjectSpace.WhenCommiting<object>(ObjectModification.Updated)
                            .Do(_ => {
                                memberInfo.SetValue(currentObject, properties.JoinComma());
                                properties.Clear();
                            }));
                })
                .ToUnit())
                .PushStackFrame();
        
        private static IObservable<Unit> ShowInstanceDetailView(this XafApplication application)
            => application.WhenSetupComplete().SelectMany(_ => application.ShowInstanceDetailView(application.TypesInfo
                    .PersistentTypes.Attributed<ShowInstanceDetailViewAttribute>().Types(true).Select(info => info.Type).ToArray())).ToUnit()
                    .PushStackFrame();
        
        private static IObservable<Unit> FireChanged(this XafApplication application)
            => application.WhenSetupComplete().SelectMany(_ => {
                var attributes = application.Model.BOModel.TypeInfos().AttributedMembers<FireChangedAttribute>().ToArray();
                var objectTypes = attributes.Select(t => t.memberInfo.Owner.Type).Distinct().ToArray();
                return application.WhenFrame(objectTypes)
                    .SelectUntilViewClosed(frame => {
                        var properties = attributes.Where(t => frame.View.ObjectTypeInfo.Members.Contains(t.memberInfo))
                            .SelectMany(t => t.attribute.Properties).ToArray();
                        return frame.View.ObjectSpace.WhenModifiedObjects(objectTypes, properties)
                            .ObserveOnContext().Select(o => o);
                    })
                    .ToUnit();
            })
            .PushStackFrame();
        
        private static IObservable<Unit> ReloadWhenChanged(this XafApplication application)
            => application.WhenSetupComplete().SelectMany(_ => {
                var membersToReload = application.Model.BOModel.TypeInfos().AttributedMembers<ReloadWhenChangeAttribute>().ToArray();
                return application.WhenFrame()
                    .WhenFrame(membersToReload.SelectMany(t =>t.attribute.AttributeTypes(t.memberInfo)).Distinct().ToArray())
                    .WhenFrame(frame => membersToReload.WhenFrame(frame)
                        .SelectMany(ts => application.WhenProviderCommitted(ts.Key).To(ts).SelectMany()).ObserveOnContext()
                        .Do(t => {
                            if (t.attribute.ObjectPropertyChangeMethodName != null) {
                                frame.View.CurrentObject.CallMethod(t.attribute.ObjectPropertyChangeMethodName, t.info.Name);      
                            }
                            else if (typeof(IReloadWhenChange).IsAssignableFrom(frame.View.ObjectTypeInfo.Type)) {
                                frame.View.CurrentObject.As<IReloadWhenChange>().WhenPropertyChanged(t.info.Name);
                            }
                            else if (frame.View.ObjectTypeInfo.Type.Implements("DevExpress.Xpo.IXPReceiveOnChangedFromArbitrarySource")) {
                                (frame.View.ObjectTypeInfo.Type.Method("FireChanged") ?? frame.View.ObjectTypeInfo.Type.Method("DevExpress.Xpo.IXPReceiveOnChangedFromArbitrarySource.FireChanged"))
                                    .Call(frame.View.CurrentObject, t.info.Name);
                            }
                        })
                        .ToUnit())
                    ;
            })
            .PushStackFrame();

        private static IEnumerable<Type> AttributeTypes(this ReloadWhenChangeAttribute attribute, IMemberInfo memberInfo) 
            => attribute.Types.Any()?attribute.Types: memberInfo.Owner.Type.RealType().YieldItem();

        private static IObservable<IGrouping<Type, (Type key, IMemberInfo info, ReloadWhenChangeAttribute attribute)>> WhenFrame(
            this IEnumerable<(ReloadWhenChangeAttribute attribute, IMemberInfo info)> membersToReload, Frame frame) 
            => membersToReload.Where(t => t.info.Owner == frame.View.ObjectTypeInfo||t.attribute.Types.Any(type => type.IsAssignableFrom(frame.View.ObjectTypeInfo.Type)))
                .SelectMany(t => t.attribute.AttributeTypes(t.info).Select(type => (key:type,t.info,t.attribute)))
                .GroupBy(t => t.key).ToNowObservable();

        private static IObservable<Unit> MergedExtraEmbeddedModels(this ApplicationModulesManager manager) 
            => manager.WhereApplication().ToObservable()
                .SelectMany(application => application.WhenCreateCustomUserModelDifferenceStore()
                    .DoItemResilient(t => {
                        var models = t.application.Modules.SelectMany(m => m.EmbeddedModels().Select(tuple => tuple with { id = $"{m.Name},{tuple.id}" }))
                            .Where(tuple => {
                                var pattern = ConfigurationManager.AppSettings["EmbeddedModels"]??@"(\.MDO)|(\.RDO)";
                                return !Regex.IsMatch(tuple.id, pattern, RegexOptions.Singleline);
                            })
                            .ToArray();
                        foreach (var model in models){
                            t.e.AddExtraDiffStore(model.id, new StringModelStore(model.model));
                        }

                        if (models.Any()){
                            t.e.AddExtraDiffStore("After Setup", new ModelStoreBase.EmptyModelStore());
                        }
                    })).ToUnit()
                    .PushStackFrame();

        private static IObservable<Unit> SetupPropertyEditorParentView(this ApplicationModulesManager applicationModulesManager) 
            => applicationModulesManager.WhereApplication().ToObservable().SelectMany(application => application.SetupPropertyEditorParentView())
                .PushStackFrame();

        
        internal static IObservable<ApplicationModulesManager> ApplicationModulesManager => ApplicationModulesManagerSubject.AsObservable();
    }

}