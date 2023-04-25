using DevExpress.EasyTest.Framework;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using DevExpress.ExpressApp.EasyTest.BlazorAdapter;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions;

namespace Xpand.TestsLib.Blazor {
	public static class Extensions {
		public static void UseSecuredProvider(this XafApplication application) {
			var providerContainer = application.ServiceProvider.GetService<IObjectSpaceProviderContainer>();
			var securedObjectSpaceProvider = new SecuredObjectSpaceProvider((ISelectDataSecurityProvider)application.Security,application.ObjectSpaceProvider.DataStoreProvider());
			providerContainer.Clear();
			providerContainer.AddObjectSpaceProvider(securedObjectSpaceProvider);
		}
		
		public static IWebDriver Driver(this ICommandAdapter adapter)
			=> ((CommandAdapter)adapter).Driver;
	}
}