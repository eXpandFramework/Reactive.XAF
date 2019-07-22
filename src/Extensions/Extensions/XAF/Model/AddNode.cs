using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Source.Extensions.XAF.Model{
    internal partial class ModelExtensions{
        public static ModelNode AddNode(this IModelNode node, string id = null){
            return node.AddNode(node.ModelListType(), id);
        }

        public static ModelNode AddNode(this IModelNode node, Type type,string id=null){
            return node.AddNode(XafTypesInfo.Instance.FindTypeInfo(type),id);
        }

        public static ModelNode AddNode(this IModelNode node, ITypeInfo typeInfo,string id=null){
            var name =id?? Guid.NewGuid().ToString();
            return ((ModelNode) node).AddNode(name, typeInfo.Type);
        }


    }
}