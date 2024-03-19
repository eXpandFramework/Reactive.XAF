using DevExpress.ExpressApp.Model;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        public static IModelListView ToListView(this IModelView modelView) => modelView.Cast<IModelListView>();
    }
}