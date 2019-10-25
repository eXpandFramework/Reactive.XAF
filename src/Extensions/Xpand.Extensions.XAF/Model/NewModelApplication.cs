using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.Model{
    public static partial class ModelExtensions{
        public static ModelApplicationBase NewModelApplication(this ModelNode application, string id){
            var modelApplication = application.CreatorInstance.CreateModelApplication();
            modelApplication.Id = id;
            return modelApplication;
        }

        public static ModelApplicationBase NewModelApplication(this IModelApplication application, string id){
            return ((ModelApplicationBase) application).NewModelApplication(id);
        }
    }
}