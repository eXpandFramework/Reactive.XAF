
using System;
using Fasterflect;

namespace Xpand.Extensions.AppDomainExtensions{
	public static partial class AppDomainExtensions{
		public static bool IsHosted(this AppDomain domain) =>
			domain.AssemblySystemWeb()?.GetType("System.Web.HttpContext").GetPropertyValue("Current") != null;
	}
}