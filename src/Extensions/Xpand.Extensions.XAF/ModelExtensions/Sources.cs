using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        public static IModelSources Sources(this IModelApplication application) => ((IModelSources)application);
    }
}