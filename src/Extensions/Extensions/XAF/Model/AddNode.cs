using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Source.Extensions.XAF.Model{
    internal partial class ModelExtensions{
        public static ModelNode AddNode(this IModelNode node, Type type,string id=null){
            return node.AddNode(XafTypesInfo.Instance.FindTypeInfo(type),id);
        }

        public static ModelNode AddNode(this IModelNode node, ITypeInfo typeInfo,string id=null){
            var name =id?? GetName(typeInfo);
            return ((ModelNode) node).AddNode(name, typeInfo.Type);
        }

        private static string GetName(ITypeInfo typeInfo){
            var displayNameAttribute = typeInfo.FindAttribute<ModelDisplayNameAttribute>();
            return displayNameAttribute != null ? displayNameAttribute.ModelDisplayName : typeInfo.Type.Name.Replace("IModel", "");
        }

    }
}