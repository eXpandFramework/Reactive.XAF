using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        public static void ReadFromString(this IModelNode modelNode, string xml, string aspect = "")
            => new ModelXmlReader().ReadFromString(modelNode, aspect, xml);
    }
}