using DevExpress.ExpressApp;
using Fasterflect;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {
        public static bool IsLoggedOn(this XafApplication application)
            => (bool)application.GetFieldValue("isLoggedOn");
    }
}