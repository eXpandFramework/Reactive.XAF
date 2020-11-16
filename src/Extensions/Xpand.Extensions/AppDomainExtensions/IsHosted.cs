
using System;
using System.Linq;

namespace Xpand.Extensions.AppDomainExtensions{
	public static partial class AppDomainExtensions{
		public static bool IsHosted(this AppDomain domain) 
            => domain.Web().HttpContext() != null||domain.GetAssemblies().Any(assembly => assembly.GetName().Name=="Microsoft.AspNetCore.Hosting");
	}
}