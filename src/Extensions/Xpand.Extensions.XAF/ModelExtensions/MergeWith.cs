using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        public static IModelNode MergeWith(this IModelNode node, IModelNode sourceNode)
            => ((ModelNode) node).Merge((ModelNode) sourceNode);
    }
}