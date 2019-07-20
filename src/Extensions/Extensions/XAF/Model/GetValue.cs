using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Source.Extensions.XAF.Model{
    internal static partial class ModelExtensions{
        public static object GetValue(this IModelNode modelNode, string propertyName){
            return ((ModelNode) modelNode).GetValue(propertyName);
        }

    }
}