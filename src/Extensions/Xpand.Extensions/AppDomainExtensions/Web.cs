using System;

namespace Xpand.Extensions.AppDomainExtensions{
	public static partial class AppDomainExtensions{
		public static IAppDomainWeb Web(this AppDomain appDomain) => new AppDomainWeb(appDomain);
	}
	class AppDomainWeb:IAppDomainWeb{
		public AppDomain AppDomain{ get; }

		public AppDomainWeb(AppDomain appDomain) => AppDomain = appDomain;
	}
	public interface IAppDomainWeb{
		AppDomain AppDomain{ get; }
	}
}