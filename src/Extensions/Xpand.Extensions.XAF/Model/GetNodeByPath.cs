using System;
using System.Collections;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.Model{
    public static partial class ModelExtensions{
        public static ModelNode GetNodeByPath(this IModelNode node, string path){
            string[] separator = {"/"};
            var strArray = path.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            var node2 = strArray[0] == "Application" ? node.Root : node.GetNode(strArray[0]);
            for (var i = 1; i < strArray.Length; i++){
                if (node2 == null) return null;
                node2 = node2 is IEnumerable ? ((ModelNode) node2)[strArray[i]] : node2.GetNode(strArray[i]);
            }

            return (ModelNode) node2;
        }
    }
}