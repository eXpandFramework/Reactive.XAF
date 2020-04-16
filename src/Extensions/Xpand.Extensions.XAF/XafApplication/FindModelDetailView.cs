using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.XafApplication{
    public static partial class XafApplicationExtensions{
        public static IModelDetailView FindModelDetailView(this DevExpress.ExpressApp.XafApplication application, System.Type objectType){
            return (IModelDetailView) application.Model.Views[application.FindDetailViewId(objectType)];
        }
    }
}