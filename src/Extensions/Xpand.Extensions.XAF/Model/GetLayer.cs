using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.Model.Core;
using Fasterflect;

namespace Xpand.Extensions.XAF.Model{
    public static partial class ModelExtensions{
        public static List<ModelNode> GetLayers(this ModelApplicationBase modelApplicationBase) {
            return ((List<ModelNode>)modelApplicationBase.GetPropertyValue("Layers")).ToList();
        }

        public static ModelApplicationBase GetLayer(this ModelApplicationBase modelApplicationBase, int index) {
            return (ModelApplicationBase) ((List<ModelNode>)modelApplicationBase.GetPropertyValue("Layers"))[index];
        }
        public static ModelApplicationBase GetLayer(this ModelApplicationBase modelApplicationBase, string id){
            var modelNodeWrapper = modelApplicationBase.GetLayers().FirstOrDefault(wrapper => wrapper.Id == id);
            return (ModelApplicationBase) modelNodeWrapper;
        }
    }
}