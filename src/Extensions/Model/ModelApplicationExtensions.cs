using System.Collections.Generic;
using DevExpress.ExpressApp.Model.Core;
using Fasterflect;

namespace DevExpress.XAF.Extensions.Model{
    public static class ModelApplicationExtensions {
        public static List<ModelNode> GetLayers(this ModelApplicationBase application) {
            return ((List<ModelNode>) application.GetPropertyValue("Layers"));
        }

        public static void InsertLayer(this ModelApplicationBase application, int index,ModelApplicationBase layer) {
            application.CallMethod("InsertLayerAtInternal", layer, index);
        }
    }
}