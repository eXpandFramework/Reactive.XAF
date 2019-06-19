using System;
using System.Collections;
using System.Collections.Generic;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Source.Extensions.XAF.Model{
    internal static partial class ModelExtensions{
        public static ModelNode GetNodeByPath(this IModelNode node, string path){
            string[] separator = {"/"};
            var strArray = path.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            var node2 = strArray[0] == "Application" ? node.Root : node.GetNode(strArray[0]);
            for (var i = 1; i < strArray.Length; i++){
                if (node2 == null) return null;
                if (node2 is IEnumerable){
                    node2 = ((ModelNode) node2)[strArray[i]];
                }
                else{
                    node2 = node2.GetNode(strArray[i]);
                }
            }

            return (ModelNode) node2;
        }
    }
}