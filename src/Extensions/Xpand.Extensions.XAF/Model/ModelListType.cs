using System;
using System.Linq;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.Model{
    public partial class ModelExtensions{
        public static Type ModelListItemType(this Type type){
            return type.ModelListType().GenericTypeArguments.First();
        }

        public static Type ModelListItemType(this IModelNode modelNode){
            return modelNode.ModelListType().GenericTypeArguments.First();
        }

        public static Type ModelListType(this IModelNode modelNode){
            return modelNode.GetType().ModelListType();
        }

        public static Type ModelListType(this Type type) {
            return type.GetInterfaces().FirstOrDefault(_ =>_.IsGenericType && _.GetGenericTypeDefinition() == typeof(IModelList<>));
        }
    }
}