using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Source.Extensions.XAF.Model{
    internal partial class ModelExtensions{
        public static ModelNode AddNode(this IModelNode node, string id = null){
            var type = node.GetType().GetInterfaces().First(_ => _.IsGenericType&&_.GetGenericTypeDefinition()==typeof(IModelList<>)).GenericTypeArguments.First();
            return node.AddNode(type, id);
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