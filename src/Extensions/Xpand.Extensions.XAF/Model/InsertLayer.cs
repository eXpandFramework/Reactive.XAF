using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Fasterflect;

namespace Xpand.Extensions.XAF.Model{
    public static partial class ModelExtensions{
        public static ModelApplicationBase InsertLayer(this ModelApplicationBase application, string id){
            var modelApplication = application.CreatorInstance.CreateModelApplication();
            modelApplication.Id = id;
            application.InsertLayer(modelApplication);
            return modelApplication;
        }

        public static void InsertLayer(this ModelApplicationBase application,  ModelApplicationBase layer){
            application.InsertLayer(application.LayersCount-1,layer);
        }

        public static void InsertLayer(this ModelApplicationBase application, int index, ModelApplicationBase layer){
            application.CallMethod("InsertLayerAtInternal", layer, index);
        }
    }
}