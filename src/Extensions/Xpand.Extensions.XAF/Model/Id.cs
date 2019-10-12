using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.Model{
    public static partial class ModelExtensions{
        public static string Id(this IModelNode modelNode) {
            return ((ModelNode) modelNode).Id;
        }

    }
}