using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo.Helpers;
using Fasterflect;

namespace Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static IXpoDataStoreProvider DataStoreProvider(this IObjectSpace space)
            => (IXpoDataStoreProvider)((IObjectLayerProvider)space).ObjectLayer.GetFieldValue("dataLayer")
                .GetPropertyValue("ConnectionProvider");
    }
}