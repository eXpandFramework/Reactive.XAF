using System;
using System.Net.NetworkInformation;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.AmbientContext;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Network;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Threading;
using Xpand.Extensions.Windows;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.TestsLib.Blazor {
	
	public abstract class BlazorCommonTest : CommonTest {
		protected IHost WebHost;

		static BlazorCommonTest() {
			TestsLib.Common.Extensions.ApplicationType = typeof(TestBlazorApplication);
			Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
			AppDomain.CurrentDomain.Await(async () => await TestTracing.Use());
		}

		protected IObservable<Unit> StartTest<TStartup>(Func<BlazorApplication, IObservable<Unit>> test, Func<BlazorApplication, IObservable<Unit>> beforeSetup = null,
			Action<IServiceCollection> configureServices = null, Action<IWebHostBuilder> configureWebHostBuilder = null,Func<WebHostBuilderContext, TStartup> startupFactory=null,TimeSpan? timeOut=null) where TStartup : class
			=> StartTest("Admin", test,beforeSetup,configureServices,configureWebHostBuilder,startupFactory,timeOut);

		
		protected IObservable<Unit> StartTest<TStartup>(string user, Func<BlazorApplication, IObservable<Unit>> test,
			Func<BlazorApplication, IObservable<Unit>> beforeSetup = null, Action<IServiceCollection> configureServices = null, 
			Action<IWebHostBuilder> configureWebHostBuilder = null,Func<WebHostBuilderContext, TStartup> startupFactory=null,TimeSpan? timeOut=null)
			where TStartup : class
			=> Host.CreateDefaultBuilder().Observe()
				.Do(_ => TestContext.CurrentContext.Test.FullName.WriteSection())
				.Do(_ => Console.Out.WriteSection(TestContext.CurrentContext.Test.FullName))
				.StartTest($"http://localhost:{IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().GetRandomAvailablePort()}",
					TestBlazorAppPath(), user, test,beforeSetup,configureServices,configureWebHostBuilder,startupFactory,
					Environment.GetEnvironmentVariable("XAFTESTBrowser"), WindowPosition.FullScreen, LogContext.None,WindowPosition.BottomRight)
				.Timeout(timeOut??120.Seconds());

		public static int Port { get; set; } = 5000;

		private static string TestBlazorAppPath() {
			var testBlazorAppPath = Environment.GetEnvironmentVariable("SOURCE_DIRECTORY");
			Console.WriteLine($"SOURCE_DIRECTORY={testBlazorAppPath}");
			return testBlazorAppPath != null ? $"{testBlazorAppPath}/src/Tests/TestApplication.Blazor.Server" : "../../src/Tests/TestApplication.Blazor.Server";
		}

		public override void Dispose() {
			base.Dispose();
			CleanBlazorEnvironment();
		}

		protected void CleanBlazorEnvironment() {
			this.Await(() => WebHost.StopAsync());
			WebHost?.Dispose();
			typeof(ValueManagerContext).Field("storageHolder", Flags.StaticPrivate).SetValue(null,
				typeof(AsyncLocal<>).MakeGenericType(AppDomain.CurrentDomain.GetAssemblyType(
					"DevExpress.ExpressApp.AmbientContext.ValueManagerContext+StorageHolder")).CreateInstance());
		}

		protected BlazorApplication NewBlazorApplication(Type startupType) {
			var defaultBuilder = Host.CreateDefaultBuilder();
			WebHost = defaultBuilder
				.ConfigureWebHostDefaults(webBuilder => {
					webBuilder.UseStartup(startupType);
					webBuilder.ConfigureKestrel(options => options.ListenAnyIP(0));
				})
				.Build();
			WebHost.Start();
			var containerInitializer = WebHost.Services.GetRequiredService<IValueManagerStorageContainerInitializer>();
			containerInitializer.Initialize();
			var serviceScope = WebHost.Services.CreateScope();
			var newBlazorApplication = serviceScope.ServiceProvider.GetService<IXafApplicationProvider>()?.GetApplication();

			newBlazorApplication.WhenApplicationModulesManager().TakeFirst()
				.SelectMany(manager => manager.WhenGeneratingModelNodes<IModelViews>().TakeFirst()
					.SelectMany().OfType<IModelListView>().Where(view => view.EditorType == typeof(DxGridListEditor))
					.Do(view => view.DataAccessMode = CollectionSourceDataAccessMode.Client))
				.Subscribe();
			return newBlazorApplication;
		}
	}
}