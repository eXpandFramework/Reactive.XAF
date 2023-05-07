
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
                xmlData.Concat(new ResourcesModelStore(lastModuleAssembly).YieldItem().XmlData()).ToArray()
                    .InsertElement( list, _ => list.Count-1);
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

        
        private static IEnumerable<(XElement sourceViewElement, string targetView, Assembly assembly, string modelName, int i)> ModelDifferenceRules(
            this (Assembly assembly, XElement document, string modelName)[] xmlData) {
            var tuples = xmlData.SelectMany(t => t.document?.Descendants(nameof(IModelMergedDifference).Substring(6)))
                .Select(element => (sourceView: element?.Attribute(nameof(IModelMergedDifference.View))?.Value,
                    targetView: element?.Parent?.Parent?.Attribute("Id")?.Value)).ToArray();
            return tuples.SelectMany(t => xmlData.SelectMany((document, i) => document.document.ObjectViews(t.sourceView)
                .Select(sourceViewElement => (sourceViewElement, t.targetView, document.assembly, document.modelName, i)))).ToArray();
        }

        private static IEnumerable<XElement> ObjectViews(this XElement document, string sourceView) 
            => document.Descendants("DetailView").Concat(document.Descendants("ListView"))
                .Where(xElement => xElement.Attribute("Id")?.Value == sourceView);

        private static void InsertElement(this List<ModelStoreBase> list, XElement targetElement,
            (XElement sourceElement, string targetView, Assembly assembly, string modelName, int i) rule, int index) {
            if (index > -1) {
                list.Insert(index , new StringModelStore("<Application><Views>".JoinString(targetElement,"</Views></Application>"),
                    rule.i.JoinString(".",rule.targetView)));
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

