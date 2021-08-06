using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Design;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Win.Core.ModelEditor;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using Xpand.XAF.ModelEditor.ModelDifference;
using Xpand.XAF.ModelEditor.WinDesktop.ModelDifference;

// ReSharper disable once CheckNamespace
namespace Xpand.XAF.ModelEditor {
    public class ModelControllerBuilder {
        public ModelEditorViewController GetController(PathInfo pathInfo) {
            var storePath = Path.GetDirectoryName(pathInfo.LocalPath);
            var fileModelStore = new FileModelStore(storePath, Path.GetFileNameWithoutExtension(pathInfo.LocalPath));
            var applicationModulesManager = GetApplicationModulesManager(pathInfo);
            Tracing.Tracer.LogText("applicationModulesManager");
            var modelApplication = GetModelApplication(applicationModulesManager, pathInfo, fileModelStore);
            return GetController(fileModelStore, modelApplication);
        }
        ModelEditorViewController GetController(FileModelStore fileModelStore, ModelApplicationBase modelApplication) 
            => new((IModelApplication)modelApplication, fileModelStore);

        ModelApplicationBase GetModelApplication(ApplicationModulesManager applicationModulesManager, PathInfo pathInfo, FileModelStore fileModelStore) {
	        ApplicationModelManager applicationModelManager = new ApplicationModelManager();
	        applicationModelManager.Setup(XafTypesInfo.Instance, applicationModulesManager.DomainComponents,
		        applicationModulesManager.Modules, applicationModulesManager.ControllersManager.Controllers,
		        Type.EmptyTypes, fileModelStore.GetAspects(), null, null);
	        var modelApplication = applicationModelManager.CreateModelApplication(new ModelApplicationBase[0]);
	        AddLayers(modelApplication, applicationModulesManager, pathInfo);
	        Tracing.Tracer.LogText("AddLayers");
	        ModelApplicationBase lastLayer = modelApplication.CreatorInstance.CreateModelApplication();
	        fileModelStore.Load(lastLayer);
	        ModelApplicationHelper.AddLayer(modelApplication, lastLayer);
	        return modelApplication;
        }

        ApplicationModulesManager GetApplicationModulesManager(PathInfo pathInfo) {
            var designerModelFactory = new DesignerModelFactory();
            ReflectionHelper.Reset();
            XafTypesInfo.HardReset();
            XpoTypesInfoHelper.ForceInitialize();
            if (pathInfo.IsApplicationModel) {
                var assembliesPath = Path.GetDirectoryName(pathInfo.AssemblyPath);
                var application = designerModelFactory.CreateApplicationFromFile(pathInfo.AssemblyPath, assembliesPath);
                InitializeTypeInfoSources(application.Modules,assembliesPath);
                var applicationModulesManager = designerModelFactory.CreateModulesManager(application, null, assembliesPath);
                return applicationModulesManager;
            }
            var moduleFromFile = designerModelFactory.CreateModuleFromFile(pathInfo.AssemblyPath, Path.GetDirectoryName(pathInfo.AssemblyPath));
            return designerModelFactory.CreateModulesManager(moduleFromFile, pathInfo.AssemblyPath);
        }

        private void InitializeTypeInfoSources(IList<ModuleBase> modules, string assembliesPath) {
            DefaultTypesInfoInitializer.Initialize(
                (TypesInfo)XafTypesInfo.Instance,
                baseType => GetRegularTypes(modules).Where(baseType.IsAssignableFrom),
                (assemblyName, typeName) => DefaultTypesInfoInitializer.CreateTypesInfoInitializer(assembliesPath, assemblyName, typeName));
        }

        private IEnumerable<Type> GetRegularTypes(IList<ModuleBase> modules) {
            List<Type> types = new List<Type>();
            foreach(ModuleBase module in modules) {
                IEnumerable<Type> regularTypes = ModuleHelper.GetRegularTypes(module);
                if(regularTypes != null) {
                    types.AddRange(regularTypes);
                }
            }
            return types;
        }

        public static IEnumerable<Type> ParentTypes(Type type){
            if (type == null){
                yield break;
            }
            foreach (var i in type.GetInterfaces()){
                yield return i;
            }
            var currentBaseType = type.BaseType;
            while (currentBaseType != null){
                yield return currentBaseType;
                currentBaseType= currentBaseType.BaseType;
            }
        }

        public static bool InheritsFrom(Type type, string typeName) => type
            .FullName==typeName|| ParentTypes(type).Select(_ => _.FullName).Any(s => typeName.Equals(s,StringComparison.Ordinal));

        public static bool InheritsFrom(Type type, Type baseType){
            if (type == null){
                return false;
            }

            if (type == baseType){
                return true;
            }
            if (baseType == null){
                return type.IsInterface || type == typeof(object);
            }
            if (baseType.IsInterface){
                return type.GetInterfaces().Contains(baseType);
            }
            var currentType = type;
            while (currentType != null){
                if (currentType.BaseType == baseType){
                    return true;
                }
                currentType = currentType.BaseType;
            }
            return false;
        }

        void AddLayers(ModelApplicationBase modelApplication, ApplicationModulesManager applicationModulesManager, PathInfo pathInfo) {
            var resourceModelCollector = new ResourceModelCollector();
            var resourceInfos = resourceModelCollector.Collect(applicationModulesManager.Modules.Select(@base => @base.GetType().Assembly), null).Where(pair => !MatchLastLayer(pair, pathInfo));
            AddLayersCore(resourceInfos, modelApplication);
            ModelApplicationBase lastLayer = modelApplication.CreatorInstance.CreateModelApplication();
            ModelApplicationHelper.AddLayer(modelApplication, lastLayer);
        }

        bool MatchLastLayer(KeyValuePair<string, ResourceInfo> pair, PathInfo pathInfo) {
            var name = pair.Key.EndsWith(ModelStoreBase.ModelDiffDefaultName) ? ModelStoreBase.ModelDiffDefaultName : pair.Key.Substring(pair.Key.LastIndexOf(".", StringComparison.Ordinal) + 1);
            bool nameMatch = (name.EndsWith(Path.GetFileNameWithoutExtension(pathInfo.LocalPath) + ""));
            bool assemblyMatch = Path.GetFileNameWithoutExtension(pathInfo.AssemblyPath) == pair.Value.AssemblyName;
            return nameMatch && assemblyMatch;
        }

        void AddLayersCore(IEnumerable<KeyValuePair<string, ResourceInfo>> layers, ModelApplicationBase modelApplication) {
            IEnumerable<KeyValuePair<string, ResourceInfo>> keyValuePairs = layers;
            foreach (var pair in keyValuePairs) {
                ModelApplicationBase layer = modelApplication.CreatorInstance.CreateModelApplication();
                layer.Id = pair.Key;
                ModelApplicationHelper.AddLayer(modelApplication, layer);
                var modelXmlReader = new ModelXmlReader();
                foreach (var aspectInfo in pair.Value.AspectInfos) {
                    modelXmlReader.ReadFromString(layer, aspectInfo.AspectName, aspectInfo.Xml);
                }
            }
        }
    }
}