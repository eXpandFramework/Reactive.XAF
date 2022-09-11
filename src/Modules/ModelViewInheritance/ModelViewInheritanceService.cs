#if !XAF201 &&!XAF192
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Xml.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using HarmonyLib;
using Swordfish.NET.Collections.Auxiliary;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.AppDomainExtensions;
using Xpand.Extensions.XAF.Harmony;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.ModelViewInheritance {
    class StringModelStore:DevExpress.ExpressApp.StringModelStore {
        public StringModelStore(string xml, string name) : base(xml) => Name = name;
        public override string Name { get; }
        public override string ToString() => Name;
    }
    public static class ModelViewInheritanceService {
        static ModelViewInheritanceService() 
            => new HarmonyMethod(typeof(ModelViewInheritanceService), nameof(CreateUnchangeableLayer))
                .PreFix(typeof(ApplicationModelManager).Method(nameof(CreateUnchangeableLayer)), false);

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.WhenExtendingModel()
                .Do(extenders => extenders.Add<IModelObjectView, IModelObjectViewMergedDifferences>()).ToUnit();

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static bool CreateUnchangeableLayer(ApplicationModelManager __instance,ref 
            ModelStoreBase[] modelDifferenceStores, bool cacheApplicationModelDifferences, ICollection<ModelStoreBase> applicationModelDifferenceStores) {
            var xmlData = modelDifferenceStores.OfType<ResourcesModelStore>().XmlData().ToArray();
            var list = modelDifferenceStores.ToList();
            xmlData.InsertElement( list, list.FindIndex);
            if (AppDomain.CurrentDomain.IsDesignTime()) {
                var lastModuleAssembly = ((IEnumerable<ModuleBase>)__instance.GetFieldValue("modules")).Last().GetType().Assembly;
                var lastLayerXmlData = new ResourcesModelStore(lastModuleAssembly).YieldItem().XmlData().ToArray();
                lastLayerXmlData.InsertElement( list, _ => list.Count-1);
            }
            modelDifferenceStores = list.ToArray();
            return true;
        }

        private static void InsertElement(this (Assembly assembly, XElement document, string modelName)[] xmlData,
            List<ModelStoreBase> list, Func<(XElement sourceElement, string targetView, Assembly assembly, string modelName, int i),int> insertPosition) {
            var rules = xmlData.ModelDifferenceRules();
            rules.ForEach(rule => list.InsertElement(rule.TargetElement(), rule, insertPosition(rule)));
        }

        private static XElement TargetElement(this (XElement sourceElement, string targetView, Assembly assembly, string modelName, int) rule) {
            var targetElement = new XElement(rule.sourceElement);
            targetElement.SetAttributeValue("Id", rule.targetView);
            return targetElement;
        }

        
        private static IEnumerable<(XElement sourceElement, string targetView, Assembly assembly, string modelName, int i)> ModelDifferenceRules(
            this (Assembly assembly, XElement document, string modelName)[] xmlData) 
            => xmlData.SelectMany(t => t.document?.Descendants(nameof(IModelMergedDifference).Substring(6)))
                .Select(element => (sourceView: element?.Attribute(nameof(IModelMergedDifference.View))?.Value,
                    targetView: element?.Parent?.Parent?.Attribute("Id")?.Value))
                .SelectMany(t => xmlData.SelectMany((document,i) => document.document?.Descendants("DetailView")
                    .Concat(document.document.Descendants("ListView")).Where(xElement => xElement.Attribute("Id")?.Value==t.sourceView)
                    .Select(sourceElement => (sourceElement,t.targetView,document.assembly,document.modelName,i))));

        private static void InsertElement(this List<ModelStoreBase> list, XElement targetElement,
            (XElement sourceElement, string targetView, Assembly assembly, string modelName, int i) rule, int index) {
            if (index > -1) {
                list.Insert(index , new StringModelStore($"<Application><Views>{targetElement}</Views></Application>",
                    $"{rule.i}.{rule.targetView}"));    
            }
        }

        private static int FindIndex(this List<ModelStoreBase> list,
            (XElement sourceElement, string targetView, Assembly assembly, string modelName, int i) rule) 
            => list.FindIndex(store =>
                store is ResourcesModelStore resourcesModelStore &&
                (Assembly)resourcesModelStore.GetFieldValue("assembly") == rule.assembly &&
                (string)resourcesModelStore.GetFieldValue("modelDesignedDiffsName") == rule.modelName);
        
        private static IEnumerable<(Assembly assembly, XElement document, string modelName)> XmlData(this IEnumerable<ResourcesModelStore> resourcesModelStores) 
            => resourcesModelStores
                .Select(store => {
                    var assembly = ((Assembly) store.GetFieldValue("assembly"));
                    var value = (string) store.GetFieldValue("modelDesignedDiffsName");
                    var name = assembly.GetManifestResourceNames().FirstOrDefault(s => s.EndsWith(value));
                    return name != null ? (stream:assembly.GetManifestResourceStream(name),assembly,modelName:value) : default;
                })
                .WhereNotDefault()
                .Select(t => (t.assembly,document:XElement.Load(t.stream),t.modelName));
    }
}

#else
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Xml.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using HarmonyLib;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.XAF.AppDomainExtensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.ModelViewInheritance {
    class StringModelStore:DevExpress.ExpressApp.StringModelStore {
        public StringModelStore(string xml, string name) : base(xml) => Name = name;
        public override string Name { get; }
        public override string ToString() => Name;
    }
    public static class ModelViewInheritanceService {
        static ModelViewInheritanceService() {
            AppDomain.CurrentDomain.Patch(harmony => {
                var original = typeof(ApplicationModelManager).Method("CollectModelStores",Flags.StaticAnyVisibility);
                var postfix = new HarmonyMethod(typeof(ModelViewInheritanceService),nameof(CollectModelStores));
                harmony.Patch(original,postfix:postfix);
            });
        }

        public static IObservable<ModelInterfaceExtenders> Connect(this ApplicationModulesManager manager) 
            => manager.WhenExtendingModel()
                .Do(extenders => extenders.Add<IModelObjectView, IModelObjectViewMergedDifferences>());

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static void CollectModelStores(ref ModelStoreBase[] __result) {
            var streams = __result.OfType<ResourcesModelStore>()
                .Select(store => {
                    var assembly = ((Assembly) store.GetFieldValue("assembly"));
                    var value = (string) store.GetFieldValue("modelDesignedDiffsName");
                    var name = assembly.GetManifestResourceNames().FirstOrDefault(s => s.EndsWith(value));
                    return name != null ? (stream:assembly.GetManifestResourceStream(name),assembly,modelName:value) : default;
                })
                .WhereNotDefault();
            var documents = streams.Select(t => (t.assembly,document:XElement.Load(t.stream),t.modelName)).ToArray();
            var rules = documents
                .SelectMany(t => t.document?.Descendants(nameof(IModelMergedDifference).Substring(6)))
                .Select(element => (sourceView: element?.Attribute(nameof(IModelMergedDifference.View))?.Value,
                    targetView: element?.Parent?.Parent?.Attribute("Id")?.Value))
                .SelectMany(t => documents.SelectMany((document,i) => document.document?.Descendants("DetailView")
                    .Concat(document.document.Descendants("ListView")).Where(xElement => xElement.Attribute("Id")?.Value==t.sourceView)
                    .Select(sourceElement => (sourceElement,t.targetView,document.assembly,document.modelName,i))));
            var list = __result.ToList();
            foreach (var rule in rules) {
                var targetElement = new XElement(rule.sourceElement);
                targetElement.SetAttributeValue("Id", rule.targetView);
                var index = list.ToList()
                    .FindIndex(store => store is ResourcesModelStore resourcesModelStore &&
                                        (Assembly) resourcesModelStore.GetFieldValue("assembly") == rule.assembly &&
                                        (string) resourcesModelStore.GetFieldValue("modelDesignedDiffsName") == rule.modelName);
                var xml = $"<Application><Views>{targetElement}</Views></Application>";
                list.Insert(index+1,new StringModelStore(xml,$"{rule.i}.{rule.targetView}"));
            }
            __result = list.ToArray();

        }

    }
}
#endif