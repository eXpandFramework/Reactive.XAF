using DevExpress.EasyTest.Framework;
using DevExpress.ExpressApp.EasyTest.BlazorAdapter;
using OpenQA.Selenium;

namespace Xpand.TestsLib.Blazor {
	public static class Extensions {
		public static IWebDriver Driver(this ICommandAdapter adapter)
			=> ((CommandAdapter)adapter).Driver;
	}
}