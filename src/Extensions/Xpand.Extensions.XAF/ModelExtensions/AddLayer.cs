using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        public static void Add(this ModelNodesGeneratorUpdaters updaters, params IModelNodesGeneratorUpdater[] nodeUpdaters)
            => nodeUpdaters.Do(updaters.Add).Enumerate();
        
        public static void AddLayer(this ModelApplicationBase application, ModelNode layer)
            => ModelApplicationHelper.AddLayer(application, (ModelApplicationBase) layer);

        public static void AddLayerBeforeLast(this ModelApplicationBase application, ModelApplicationBase layer) {
            ModelApplicationBase lastLayer = application.LastLayer;
            ModelApplicationHelper.RemoveLayer(application);
            ModelApplicationHelper.AddLayer(application, layer);
            ModelApplicationHelper.AddLayer(application, lastLayer);
        }
    }
}