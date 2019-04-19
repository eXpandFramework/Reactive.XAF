using System;
using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Fasterflect;

namespace Xpand.Source.Extensions.XAF.Model{
    internal static partial class ModelExtensions{
        public static object GetValue(this IModelNode modelNode, string propertyName){
            var modelValueInfo = GetModelValueInfo(modelNode, propertyName);
            if (modelValueInfo.valueInfo == null)
                return null;
            return GetValue(modelValueInfo.Item2, propertyName.Split('.').Last(), modelValueInfo.Item1.PropertyType);
        }

        public static (ModelValueInfo valueInfo,IModelNode node) GetModelValueInfo(this IModelNode modelNode, string propertyName) {
            if (propertyName.Contains(".")){
                var split = propertyName.Split('.');
                var strings = string.Join(".", split.Skip(1));
                var node = ((IModelNode) modelNode.GetValue(split.First()));
                return node.GetModelValueInfo(strings);
            }
            var modelValueInfo = ((ModelNode) modelNode).GetValueInfo(propertyName);
            return (modelValueInfo, modelNode);
        }
        public static object GetValue(this IModelNode modelNode,string propertyName,Type propertyType) {
            return modelNode.CallMethod(new[]{propertyType}, "GetValue", propertyName);
        }
    }
}