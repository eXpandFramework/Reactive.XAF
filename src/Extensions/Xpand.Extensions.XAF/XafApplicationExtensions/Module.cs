using System.Linq;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {
        public static T Module<T>(this XafApplication application) where T : ModuleBase 
            => application.Modules.OfType<T>().FirstOrDefault();
    }
}