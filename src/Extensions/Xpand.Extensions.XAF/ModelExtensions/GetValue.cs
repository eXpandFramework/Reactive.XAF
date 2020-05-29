using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public static partial class ModelExtensions{
        public static object GetValue(this IModelNode modelNode, string propertyName) => ((ModelNode) modelNode).GetValue(propertyName);
    }
}