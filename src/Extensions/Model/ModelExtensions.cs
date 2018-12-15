using System;
using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Utils;
using DevExpress.XAF.Extensions.Convert;
using Fasterflect;

namespace DevExpress.XAF.Extensions.Model {
    public static class ModelNodeExtensions {
        public static ModelNode GetNodeByPath(this IModelNode node, string path) {
            const string rootNodeName = "Application";
            Guard.ArgumentNotNull(node, "node");
            Guard.ArgumentNotNullOrEmpty(path, "path");
            string[] items = path.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            var sourceNode = (items[0] == rootNodeName ? node.Root : node.GetNode(items[0])) ;
            for (int i = 1; i < items.Length; ++i) {
                if (sourceNode == null) {
                    return null;
                }
                sourceNode = sourceNode.GetNode(items[i]);
            }
            return (ModelNode)sourceNode;
        }

        public static string Xml(this IModelNode modelNode) {
            return ((ModelNode) modelNode).Xml;
        }

        public static TNode GetParent<TNode>(this IModelNode modelNode) where TNode : class, IModelNode{
            if (modelNode is TNode node)
                return node;
            var parent = modelNode.Parent;
            while (!(parent is TNode)) {
                parent = parent.Parent;
                if (parent == null)
                    break;
            }
            return (TNode) parent;
        }

        public static object GetValue(this IModelNode modelNode,string propertyName,Type propertyType) {
            return modelNode.CallMethod(new[]{propertyType}, "GetValue", propertyName);
        }

        public static void SetValue(this IModelNode modelNode,string propertyName,Type propertyType,object value){
            if (propertyType==null){
                var modelValueInfo = modelNode.GetModelValueInfo(propertyName).Item1;
                var changedValue = modelValueInfo.ChangedValue(value, modelValueInfo.PropertyType);
                modelNode.CallMethod(new[] { modelValueInfo.PropertyType }, "SetValue", propertyName, changedValue);
            }
            else
                modelNode.CallMethod(new[] { propertyType }, "SetValue", propertyName, value);
        }

        public static object GetValue(this IModelNode modelNode, string propertyName){
            var modelValueInfo = GetModelValueInfo(modelNode, propertyName);
            return GetValue(modelValueInfo.Item2, propertyName.Split('.').Last(), modelValueInfo.Item1.PropertyType);
        }

        public static object ChangedValue(this ModelValueInfo modelValueInfo,object value, Type destinationType){
            var typeConverter = modelValueInfo.TypeConverter;
            return typeConverter != null ? typeConverter.ConvertFrom(value) : value.Change(destinationType);
        }

        public static Tuple<ModelValueInfo,IModelNode> GetModelValueInfo(this IModelNode modelNode, string propertyName) {
            if (propertyName.Contains(".")){
                var split = propertyName.Split('.');
                var strings = string.Join(".", split.Skip(1));
                var node = ((IModelNode) modelNode.GetValue(split.First()));
                return node.GetModelValueInfo(strings);
            }
            var modelValueInfo = ((ModelNode) modelNode).GetValueInfo(propertyName);
            return new Tuple<ModelValueInfo, IModelNode>(modelValueInfo, modelNode);
        }

        public static string Id(this IModelNode modelNode) {
            return ((ModelNode) modelNode).Id;
        }

        public static bool IsNewNode(this IModelNode modelNode) {
            return ((ModelNode) modelNode).IsNewNode;
        }
    }
}
