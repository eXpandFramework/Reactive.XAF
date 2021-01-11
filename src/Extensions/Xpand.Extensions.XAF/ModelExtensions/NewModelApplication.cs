using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public static partial class ModelExtensions{
        public static ModelApplicationBase NewModelApplication(this ModelNode application, string id=null){
            var modelApplication = application.CreatorInstance.CreateModelApplication();
            if (id != null) modelApplication.Id = id;
            return modelApplication;
        }

        public static ModelApplicationBase NewModelApplication(this IModelApplication application, string id=null) 
            => ((ModelApplicationBase) application).NewModelApplication(id);
    }
}