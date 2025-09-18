using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        public static IModelListView ToListView(this IModelView modelView) => (IModelListView)modelView;
    }
}