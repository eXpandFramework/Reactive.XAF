using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {
        public static IObjectSpace CreateObjectSpace(this XafApplication application, bool useObjectSpaceProvider)
            => application.ObjectSpaceProvider.CreateObjectSpace();
    }
}