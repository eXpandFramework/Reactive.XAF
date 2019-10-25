using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.Model{
    public static partial class ModelExtensions{
        public static void RemoveLayer(this ModelApplicationBase application){
            ModelApplicationHelper.RemoveLayer(application);
        }

        public static void RemoveLayer(this ModelApplicationBase application, string id){
            RefreshLayers(application, @base => @base.Id == id ? null : @base);
        }
    }
}