using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.Model{
    public static partial class ModelExtensions{
        public static object GetValue(this IModelNode modelNode, string propertyName){
            return ((ModelNode) modelNode).GetValue(propertyName);
        }

    }
}