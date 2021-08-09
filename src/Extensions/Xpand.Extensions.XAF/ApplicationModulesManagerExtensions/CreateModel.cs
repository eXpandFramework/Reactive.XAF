using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model.Core;
using Fasterflect;

namespace Xpand.Extensions.XAF.ApplicationModulesManagerExtensions {
    public static partial class ApplicationModulesManagerExtensions {
        public static ModelApplicationBase CreateModel(this ApplicationModulesManager manager,
            IEnumerable<string> aspects, ModelStoreBase modelDifferenceStore = null) {
            ApplicationModelManager applicationModelManager = new ApplicationModelManager();

            var application = manager.Application();
            var modelAssemblyFilePath = application.CallMethod("GetModelAssemblyFilePath") as string;
            applicationModelManager.Setup(XafTypesInfo.Instance, manager.DomainComponents, manager.Modules,
                manager.ControllersManager.Controllers,
                application.ResourcesExportedToModel, aspects, modelDifferenceStore, modelAssemblyFilePath);
            return applicationModelManager.CreateModelApplication(new ModelApplicationBase[0]);
        }
    }
}