using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Source.Extensions.XAF.Model{
    internal static partial class ModelExtensions{
        public static object GetValue(this IModelNode modelNode, string propertyName){
            return ((ModelNode) modelNode).GetValue(propertyName);
        }

//        public static (ModelValueInfo valueInfo,IModelNode node) GetModelValueInfo(this IModelNode modelNode, string propertyName) {
//            if (propertyName.Contains(".")){
//                var split = propertyName.Split('.');
//                var strings = string.Join(".", split.Skip(1));
//                var node = ((IModelNode) modelNode.GetValue(split.First()));
//                return node.GetModelValueInfo(strings);
//            }
//            var modelValueInfo = ((ModelNode) modelNode).GetValueInfo(propertyName);
//            return (valueInfo:modelValueInfo, modelNode);
//        }
//        public static object GetValue(this IModelNode modelNode,string propertyName,Type propertyType) {
//            return ((ModelNode) modelNode).GetValue(propertyName)
////            return modelNode.CallMethod(new[]{propertyType}, "GetValue", propertyName);
//        }
    }
}