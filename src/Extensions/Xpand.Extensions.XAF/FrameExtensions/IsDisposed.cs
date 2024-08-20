using DevExpress.ExpressApp;
using Fasterflect;

namespace Xpand.Extensions.XAF.FrameExtensions {
    public partial class FrameExtensions {
        public static bool IsDisposed(this Frame source) 
            => (bool)source.GetPropertyValue("IsDisposed");
    }
}