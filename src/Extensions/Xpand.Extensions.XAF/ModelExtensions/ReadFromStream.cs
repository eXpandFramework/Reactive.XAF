using System.IO;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.StreamExtensions;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        public static void ReadFromStream(this IModelNode modelNode, Stream stream, string aspect = "")
            => new ModelXmlReader().ReadFromString(modelNode, aspect, stream.ReadToEnd());
    }
}