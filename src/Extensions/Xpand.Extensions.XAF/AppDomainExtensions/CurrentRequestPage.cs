namespace Xpand.Extensions.XAF.AppDomainExtensions{
    public static partial class AppDomainExtensions{
        public static object CurrentRequestPage(this IXAFAppDomain xafAppDomain) => xafAppDomain.WebWindowType()
            ?.GetProperty("CurrentRequestPage")?.GetValue(null);
    }
}