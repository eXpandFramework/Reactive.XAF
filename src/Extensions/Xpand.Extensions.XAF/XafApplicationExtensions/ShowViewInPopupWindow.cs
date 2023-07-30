using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {
        public static void ShowViewInPopupWindow(this XafApplication application, object instance)
            => application.ShowViewStrategy.ShowViewInPopupWindow(
                application.NewDetailView(space => space.GetObject(instance)));
    }
}