using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Xpo;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.TestsLib.Blazor {
	public class XpoDataStoreProviderAccessor {
		public IXpoDataStoreProvider DataStoreProvider { get; set; }
	}

	public class TestBlazorApplication : BlazorApplication, ITestApplication {
		public TestBlazorApplication() {
			DatabaseUpdateMode=DatabaseUpdateMode.UpdateDatabaseAlways;
			CheckCompatibilityType=CheckCompatibilityType.DatabaseSchema;
			this.AlwaysUpdateOnDatabaseVersionMismatch().TakeFirst().Subscribe();
		}

		[SuppressMessage("ReSharper", "UnusedParameter.Local")]
		public TestBlazorApplication(Type sutModule, bool transmitMessage = true, bool handleExceptions = true) :
			this() {
			TransmitMessage = transmitMessage;
			SUTModule = sutModule;
			TraceClientConnected = this.ClientConnect();
			TraceClientBroadcast = this.ClientBroadcast();
			this.WhenSetupComplete().TakeFirst()
				.Select(application => application.ObjectSpaceProvider.DataStoreProvider())
				.Do(provider => {
					var dataStoreProviderAccessor = ServiceProvider.GetService<XpoDataStoreProviderAccessor>();
					if (dataStoreProviderAccessor != null) dataStoreProviderAccessor.DataStoreProvider = provider;
				})
				.Subscribe();
		}

		public bool TransmitMessage { get; }

		public IObservable<Unit> TraceClientBroadcast { get; set; }
		public IObservable<Unit> TraceClientConnected { get; set; }

		public Type SUTModule { get; }


		protected override void OnCreateCustomObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args) {
			base.OnCreateCustomObjectSpaceProvider(args);
			if (!args.ObjectSpaceProviders.OfType<XPObjectSpaceProvider>().Any()) {
				var provider = ServiceProvider.GetRequiredService<XpoDataStoreProviderAccessor>().DataStoreProvider;
				args.ObjectSpaceProvider = this.NewObjectSpaceProvider(provider);
			}
			args.ObjectSpaceProviders.Add(ServiceProvider.GetService<NonPersistentObjectSpaceProvider>() ??
			                              new NonPersistentObjectSpaceProvider(TypesInfo, null));
		}

		protected override string GetModelCacheFileLocationPath() => null;

		protected override string GetDcAssemblyFilePath() => null;

		protected override string GetModelAssemblyFilePath() =>
			$@"{AppDomain.CurrentDomain.ApplicationPath()}\ModelAssembly{Guid.NewGuid()}.dll";
	}
}