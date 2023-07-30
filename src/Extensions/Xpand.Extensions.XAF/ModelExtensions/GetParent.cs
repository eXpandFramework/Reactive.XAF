using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public static partial class ModelExtensions{
        public static TNode GetParent<TNode>(this IModelNode modelNode) where TNode : class 
            => modelNode.Parent as TNode ?? modelNode.Parent?.GetParent<TNode>();
    }
}
