using DevExpress.ExpressApp;
using DevExpress.ExpressApp.MultiTenancy;
using DevExpress.Persistent.Base.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static string TenantName(this IObjectSpace objectSpace)
            => objectSpace.ServiceProvider.GetRequiredService<ITenantProvider>().TenantName;
    }
}