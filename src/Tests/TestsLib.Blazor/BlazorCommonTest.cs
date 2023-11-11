using System;
using System.Reactive.Linq;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.AmbientContext;
using DevExpress.ExpressApp.Blazor.Editors.Grid;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Threading;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.TestsLib.Blazor {
	public class BlazorCommonTest : CommonTest {
		protected IHost WebHost;


		static BlazorCommonTest() {
			TestsLib.Common.Extensions.ApplicationType = typeof(TestBlazorApplication);
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
					"DevExpress.ExpressApp.Blazor.AmbientContext.ValueManagerContext+StorageHolder")).CreateInstance());
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
			var newBlazorApplication = WebHost.Services.GetService<IXafApplicationProvider>()?.GetApplication();

			newBlazorApplication.WhenApplicationModulesManager().TakeFirst()
				.SelectMany(manager => manager.WhenGeneratingModelNodes<IModelViews>().TakeFirst()
					.SelectMany().OfType<IModelListView>().Where(view => view.EditorType == typeof(GridListEditor))
					.Do(view => view.DataAccessMode = CollectionSourceDataAccessMode.Client))
				.Subscribe();
			return newBlazorApplication;
		}
	}
}