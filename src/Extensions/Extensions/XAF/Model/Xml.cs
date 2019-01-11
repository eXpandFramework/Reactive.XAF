using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Source.Extensions.XAF.Model{
    internal static partial class Extensions{
        public static string Xml(this IModelNode modelNode){
            return ((ModelNode) modelNode).Xml;
        }
    }
}