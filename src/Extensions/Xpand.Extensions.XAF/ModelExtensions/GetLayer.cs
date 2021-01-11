using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.Model.Core;
using Fasterflect;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public static partial class ModelExtensions{
        public static List<ModelNode> GetLayers(this ModelApplicationBase modelApplicationBase) => ((List<ModelNode>)modelApplicationBase.GetPropertyValue("Layers"));

        public static ModelApplicationBase GetLayer(this ModelApplicationBase modelApplicationBase, int index) => 
            (ModelApplicationBase) ((List<ModelNode>)modelApplicationBase.GetPropertyValue("Layers"))[index];

        public static ModelApplicationBase GetLayer(this ModelApplicationBase modelApplicationBase, string id) => 
            (ModelApplicationBase) modelApplicationBase.GetLayers().FirstOrDefault(wrapper => wrapper.Id == id);
    }
}