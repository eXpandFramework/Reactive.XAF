using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public static partial class ModelExtensions{
        public static IModelView FindModelView(this IModelApplication modelApplication, System.String viewId) => modelApplication?.Application.Views?[viewId];
    }
}