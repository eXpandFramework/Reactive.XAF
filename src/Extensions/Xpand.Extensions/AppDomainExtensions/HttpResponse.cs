using Fasterflect;

namespace Xpand.Extensions.AppDomainExtensions{
	public static partial class AppDomainExtensions{
		public static object WriteHttpResponse(this IAppDomainWeb domainWeb,string text, bool end=false) =>
			domainWeb.HttpResponse().CallMethod("Write", text);

		public static object HttpResponse(this IAppDomainWeb domainWeb) =>
			domainWeb.HttpContext()?.GetPropertyValue("Response");
	}
}