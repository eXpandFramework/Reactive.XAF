using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public static partial class ModelExtensions{
        public static ModelApplicationBase NewModelApplication(this ModelNode application, string id){
            var modelApplication = application.CreatorInstance.CreateModelApplication();
            modelApplication.Id = id;
            return modelApplication;
        }

        public static ModelApplicationBase NewModelApplication(this IModelApplication application, string id) => ((ModelApplicationBase) application).NewModelApplication(id);
    }
}