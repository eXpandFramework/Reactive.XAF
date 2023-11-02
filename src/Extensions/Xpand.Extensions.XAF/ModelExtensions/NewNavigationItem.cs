using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public partial class ModelExtensions {
        public static IModelNavigationItem NewNavigationItem(this IModelApplication modelApplication, string defaultGroup, string viewId,string imageName=null){
            var item = ShowNavigationItemController.GenerateNavigationItem(modelApplication, defaultGroup, viewId, null, viewId, null);
            item.ImageName = imageName;
            return item;
        }
    }
}