using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.DataAccess.Excel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public partial class ModelExtensions{
        public static T EnsureNode<T>(this IModelNode node, string id) where T : class, IModelNode => node.EnsureNodes<T>(id).First();

        public static ModelNode AddNode<T>(this IModelNode node, string id,bool checkForDuplicates) where T:IModelNode 
            => checkForDuplicates && (((ModelNode)node)[id] != null) ? throw new DuplicateNameValidationException($"{node}")
                : (ModelNode)(IModelNode)node.AddNode<T>(id);

        public static IEnumerable<T> EnsureNodes<T>(this IModelNode node, params string[] ids) where T: class, IModelNode 
            => ids.Select(id => node.GetNode(id) as T ?? node.AddNode<T>());
        
        public static T EnsureNode<T>(this IModelNode node, params string[] ids) where T: class, IModelNode 
            => node.EnsureNodes<T>().First();
        
        public static ModelNode AddNode(this IModelNode node, string id = null) => node.AddNode(node.ModelListType(), id);

        public static ModelNode AddNode(this IModelNode node, Type type,string id=null) => node.AddNode(XafTypesInfo.Instance.FindTypeInfo(type),id);

        public static ModelNode AddNode(this IModelNode node, ITypeInfo typeInfo,string id=null) => ((ModelNode) node).AddNode(id?? Guid.NewGuid().ToString(), typeInfo.Type);
    }
}