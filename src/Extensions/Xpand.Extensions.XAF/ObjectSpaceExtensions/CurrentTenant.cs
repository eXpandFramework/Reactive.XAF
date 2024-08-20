using DevExpress.ExpressApp;
using DevExpress.ExpressApp.MultiTenancy;
using DevExpress.Persistent.Base.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static TTenant CurrentTenant<TTenant>(this IObjectSpace objectSpace) where TTenant : ITenant
            => objectSpace.GetObjectByKey<TTenant>(objectSpace.ServiceProvider.GetRequiredService<ITenantProvider>()
                .TenantId);
    }
}