using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo.Helpers;

namespace Xpand.Extensions.XAF.Xpo.SessionProviderExtensions {
    public static class SessionProviderExtensions {
        public static IObjectSpace ObjectSpace(this ISessionProvider provider) 
            => XPObjectSpace.FindObjectSpaceByObject(provider);
    }
}
