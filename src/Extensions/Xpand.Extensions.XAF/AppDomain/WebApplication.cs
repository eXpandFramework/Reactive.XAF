namespace Xpand.Extensions.XAF.AppDomain{
    public static partial class AppDomainExtensions{
        private static DevExpress.ExpressApp.XafApplication _webApplication;
        public static DevExpress.ExpressApp.XafApplication WebApplication(this IXAFAppDomain xafAppDomain) =>
            _webApplication ??= (DevExpress.ExpressApp.XafApplication) xafAppDomain.WebApplicationType()?.GetProperty("Instance")?.GetValue(null);
    }
}