using System.Linq;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.Model{
    public partial class ModelExtensions{
        public static System.Type ModelListItemType(this System.Type type){
            return type.ModelListType().GenericTypeArguments.First();
        }

        public static System.Type ModelListItemType(this IModelNode modelNode){
            return modelNode.ModelListType().GenericTypeArguments.First();
        }

        public static System.Type ModelListType(this IModelNode modelNode){
            return modelNode.GetType().ModelListType();
        }

        public static System.Type ModelListType(this System.Type type) {
            return type.GetInterfaces().FirstOrDefault(_ =>_.IsGenericType && _.GetGenericTypeDefinition() == typeof(IModelList<>));
        }
    }
}