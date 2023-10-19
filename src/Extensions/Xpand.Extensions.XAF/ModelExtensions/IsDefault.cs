using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        public static bool IsDefault(this IModelObjectView modelObjectView)
            => modelObjectView is IModelListView modelListView ? modelObjectView.ModelClass.DefaultListView == modelListView
                : modelObjectView.ModelClass.DefaultDetailView == modelObjectView;
    }
}