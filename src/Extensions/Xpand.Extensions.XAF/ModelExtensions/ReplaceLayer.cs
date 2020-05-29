using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public static partial class ModelExtensions{
        public static void ReplaceLayer(this ModelApplicationBase application, ModelApplicationBase layer) => 
            RefreshLayers(application, @base => application.LastLayer.Id == layer.Id ? layer : @base);
    }
}