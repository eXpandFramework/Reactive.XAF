using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.Model{
    public static partial class ModelExtensions{
        public static ModelApplicationBase Master(this IModelApplication application){
            return (ModelApplicationBase) ((ModelApplicationBase) application).Master;
        }
    }
}