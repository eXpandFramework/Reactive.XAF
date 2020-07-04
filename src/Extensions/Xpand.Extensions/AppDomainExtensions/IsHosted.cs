
using System;

namespace Xpand.Extensions.AppDomainExtensions{
	public static partial class AppDomainExtensions{
		public static bool IsHosted(this AppDomain domain) => domain.Web().HttpContext() != null;
	}
}