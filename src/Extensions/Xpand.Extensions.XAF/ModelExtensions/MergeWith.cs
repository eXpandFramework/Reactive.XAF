using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        public static IModelNode MergeWith(this IModelNode node, string xml) {
            var modelXmlReader = new ModelXmlReader();
            var modelApplicationBase = node.Application.NewModelApplication();
            modelXmlReader.ReadFromString(modelApplicationBase, "",xml);
            node.MergeWith(modelApplicationBase);
            return node;
        }
        public static IModelNode MergeWith(this IModelNode node, IModelNode sourceNode)
            => ((ModelNode) node).Merge((ModelNode) sourceNode);
    }
}