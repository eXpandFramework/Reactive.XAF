using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using Xpand.Extensions.Reactive;

namespace Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static RpcHandler<IDataLayer> HandleDataLayerRequest(this IObjectSpaceProvider objectSpaceProvider)
            => ((objectSpaceProvider as XPObjectSpaceProvider)?.DataLayer).HandleRequest();
    }
}