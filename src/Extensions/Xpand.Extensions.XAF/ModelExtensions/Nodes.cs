using System.Collections.Generic;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public static partial class ModelExtensions{
        public static IEnumerable<IModelNode> Nodes(this IModelNode node){
            for (int i = 0; i < node.NodeCount; i++){
                yield return node.GetNode(i);
            }
        }
    }
}