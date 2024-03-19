using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.ModelExtensions;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {
        public static void CreateCollectionSource(this XafApplication application, string viewId) {
            var objectType = application.Model.Views[viewId].ToListView().ModelClass.TypeInfo.Type;
            application.CreateCollectionSource(application.CreateObjectSpace(objectType), objectType, viewId);
        }
    }
}