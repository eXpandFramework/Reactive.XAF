using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.NodeGenerators;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        public static void GenerateNodes(this IModelListView listView)
            => ModelListViewNodesGenerator.GenerateNodes(listView, listView.ModelClass);
    }
}