using Fasterflect;

namespace Xpand.Extensions.AppDomainExtensions{
	public static partial class AppDomainExtensions{
        public static object HttpContext(this IAppDomainWeb domainWeb) 
            => domainWeb.SystemWebAssembly()?.GetType("System.Web.HttpContext")?.GetPropertyValue("Current");
	}
}