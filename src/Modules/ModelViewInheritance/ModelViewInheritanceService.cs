#if !XAF201 &&!XAF192
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Xml.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using HarmonyLib;
using Xpand.Extensions.Harmony;
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
        static ModelViewInheritanceService() 
            => typeof(ApplicationModelManager).Method("CreateUnchangeableLayer")
                .PatchWith(new HarmonyMethod(typeof(ModelViewInheritanceService),nameof(CreateUnchangeableLayer)),
                    new HarmonyMethod(typeof(ModelViewInheritanceService),nameof(CreateUnchangeableLayer)));

        internal static IObservable<ModelInterfaceExtenders> Connect(this ApplicationModulesManager manager) 
            => manager.WhenExtendingModel()
                .Do(extenders => extenders.Add<IModelObjectView, IModelObjectViewMergedDifferences>());

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static void CreateUnchangeableLayer(ref 
            ModelStoreBase[] modelDifferenceStores, bool cacheApplicationModelDifferences, ICollection<ModelStoreBase> applicationModelDifferenceStores) {
            var streams = modelDifferenceStores.OfType<ResourcesModelStore>()
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
            var list = modelDifferenceStores.ToList();
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
            modelDifferenceStores = list.ToArray();

        }

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