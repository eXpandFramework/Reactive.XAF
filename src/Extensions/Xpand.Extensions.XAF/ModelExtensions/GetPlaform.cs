using DevExpress.ExpressApp.Model;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        public static Platform GetPlaform(this IModelApplication application) 
            => application.Sources().Modules.GetPlatform();
    }
}