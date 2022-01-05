using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using DevExpress.EasyTest.Framework;
using DevExpress.ExpressApp.Xpo;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Xpand.Extensions.StringExtensions;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;

namespace ALL.Tests {
	public abstract class CommonTest : Xpand.TestsLib.Common.CommonTest {
		[SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
		protected async Task EasyTest<TAppAdapter>(Func<TAppAdapter> appAdapter,
			Func<TAppAdapter, string, TestApplication> applicationFactory, Func<ICommandAdapter, Task> executeCommands,
			string connectionString = null) where TAppAdapter : IApplicationAdapter {
			connectionString ??= InMemoryDataStoreProvider.ConnectionString;
			using TAppAdapter winAdapter = appAdapter();
			var testApplication = applicationFactory(winAdapter, connectionString);
			try {
				var commandAdapter = winAdapter.CreateCommandAdapter();
				commandAdapter.Execute(new LoginCommand());
				await executeCommands(commandAdapter);
			}

			finally {
				var easyTestSettingsFile = testApplication.EasyTestSettingsFile();
				if (File.Exists(easyTestSettingsFile)) {
					File.Delete(easyTestSettingsFile);
				}

				WriteLine(
					$"Finally EasyTest {GetType().Assembly.GetName().Name} {TestContext.CurrentContext.Result.Outcome.Status}");
				winAdapter.KillApplication(testApplication, KillApplicationContext.TestNormalEnded);
				if (TestContext.CurrentContext.Result.Outcome.Status != TestStatus.Passed) {
					var path = Path.GetDirectoryName(easyTestSettingsFile);
					WriteLine(path!);
					foreach (var file in Directory.GetFiles(path!, "*.log")) {
						var zipPPath = $"{Path.GetDirectoryName(file)}\\{Path.GetFileNameWithoutExtension(file)}.gz";
						try {
							File.WriteAllBytes(zipPPath, File.ReadAllText(file).GZip());
							WriteLine($"Attaching {zipPPath}");
							TestContext.AddTestAttachment(zipPPath);
						}
						catch (Exception) {
							// ignored
						}
					}
				}
			}
		}
	}
}