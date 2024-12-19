using System;
using System.Linq;
using System.Reflection;
using Fasterflect;
using MethodInvoker = Fasterflect.MethodInvoker;

namespace Xpand.Extensions.AppDomainExtensions{
	public static partial class AppDomainExtensions{
		public static MethodInvoker TypeUnitPercentage(this IAppDomainWeb appDomainWeb){
			return appDomainWeb.TypeUnit().GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static).First(info => info.Name=="Percentage").DelegateForCallMethod();
		}

		public static Type TypeUnit(this IAppDomainWeb domainWeb) =>
			domainWeb.SystemWebAssembly().GetType("System.Web.UI.WebControls.Unit");
	}
}