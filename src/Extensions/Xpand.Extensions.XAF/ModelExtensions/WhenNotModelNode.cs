using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        private static HashSet<string> _modelNodePropertyInfos;

        static ModelExtensions() {
            _modelNodePropertyInfos = new HashSet<string>(typeof(ModelNode).GetProperties().Select(info => info.Name).Concat(new []{"Removed"}));
        }
        public static IEnumerable<ModelValueInfo> WhenNotModelNode(this IEnumerable<ModelValueInfo> source) => source.Where(info => !_modelNodePropertyInfos.Contains(info.Name));
    }
}