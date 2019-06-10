using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Source.Extensions.XAF.Model{
    internal partial class ModelExtensions{
        public static string Id(this IModelNode modelNode) {
            return ((ModelNode) modelNode).Id;
        }

    }
}