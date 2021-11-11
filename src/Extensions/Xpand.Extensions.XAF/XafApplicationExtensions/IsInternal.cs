using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {
        public static readonly string ApplicationMarker = typeof(XafApplicationExtensions).Assembly.GetName().Name;
        public static bool IsInternal(this XafApplication application) 
            => ValueManager.GetValueManager<bool>(ApplicationMarker).Value;
    }
}