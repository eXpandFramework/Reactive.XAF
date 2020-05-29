using System.Linq;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public partial class ModelExtensions{
        public static System.Type ModelListItemType(this System.Type type) => type.ModelListType().GenericTypeArguments.First();

        public static System.Type ModelListItemType(this IModelNode modelNode) => modelNode.ModelListType().GenericTypeArguments.First();

        public static System.Type ModelListType(this IModelNode modelNode) => modelNode.GetType().ModelListType();

        public static System.Type ModelListType(this System.Type type) => type.GetInterfaces().FirstOrDefault(_ =>_.IsGenericType && _.GetGenericTypeDefinition() == typeof(IModelList<>));
    }
}