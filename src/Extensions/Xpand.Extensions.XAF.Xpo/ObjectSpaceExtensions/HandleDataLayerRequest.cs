using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using Xpand.Extensions.Reactive.Channels;

namespace Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static RpcHandler<IXpoDataStoreProvider> HandleDataLayerRequest(this IObjectSpaceProvider objectSpaceProvider) 
            => objectSpaceProvider.DataStoreProvider().HandleRequest();
    }
}