using System;
using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        public static Tuple<ModelValueInfo, IModelNode> GetModelValueInfo(this IModelNode modelNode,
            string propertyName) {
            if (propertyName.Contains(".")) {
                var split = propertyName.Split('.');
                var strings = string.Join(".", split.Skip(1));
                var node = ((IModelNode)modelNode.GetValue(split.First()));
                return node.GetModelValueInfo(strings);
            }

            var modelValueInfo = ((ModelNode)modelNode).GetValueInfo(propertyName);
            return new Tuple<ModelValueInfo, IModelNode>(modelValueInfo, modelNode);
        }
    }
}