using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public static partial class ModelExtensions{
        public static ModelApplicationBase Master(this IModelApplication application) 
            => (ModelApplicationBase) ((ModelApplicationBase) application).Master;
    }
}