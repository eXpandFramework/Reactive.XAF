
using System;
using System.Linq;

namespace Xpand.Extensions.AppDomainExtensions{
	public static partial class AppDomainExtensions {
		private static bool? _isHosted;

		public static void SetIsHosted(this AppDomain domain,bool value)
			=> _isHosted = value; 
		public static bool IsHosted(this AppDomain domain) 
            => _isHosted ?? domain.Web().HttpContext() != null || domain.GetAssemblies()
	            .Any(assembly => assembly.GetName().Name == "Microsoft.AspNetCore.Hosting");
	}
}