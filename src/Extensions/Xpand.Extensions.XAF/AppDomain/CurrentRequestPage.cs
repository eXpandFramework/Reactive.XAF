namespace Xpand.Extensions.XAF.AppDomain{
    public static partial class AppDomainExtensions{
        public static object CurrentRequestPage(this IXAFAppDomain xafAppDomain) => xafAppDomain.WebWindowType()
            ?.GetProperty("CurrentRequestPage")?.GetValue(null);
    }
}