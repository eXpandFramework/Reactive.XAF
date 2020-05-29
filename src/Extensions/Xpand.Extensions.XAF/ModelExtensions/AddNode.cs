using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public partial class ModelExtensions{
        public static ModelNode AddNode(this IModelNode node, string id = null) => node.AddNode(node.ModelListType(), id);

        public static ModelNode AddNode(this IModelNode node, Type type,string id=null) => node.AddNode(XafTypesInfo.Instance.FindTypeInfo(type),id);

        public static ModelNode AddNode(this IModelNode node, ITypeInfo typeInfo,string id=null) => ((ModelNode) node).AddNode(id?? Guid.NewGuid().ToString(), typeInfo.Type);
    }
}