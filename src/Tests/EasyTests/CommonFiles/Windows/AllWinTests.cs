﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ALL.Tests;
using DevExpress.EasyTest.Framework;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EasyTest.WinAdapter;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using Web.Tests;
using Win.Tests;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands.Automation;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Logger;
using CommonTest = ALL.Tests.CommonTest;

namespace ALL.Win.Tests {
	[NonParallelizable]
	public class AllWinTests : CommonTest {
#if !NETCOREAPP3_1_OR_GREATER
        [XpandTest(LongTimeout, 3)]
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Win_MicrosoftCloud_EasyTest() {
            DeleteBrowserFiles.Execute();
            await EasyTest(() => new WinAdapter(), RunWinApplication, async adapter => {
                await adapter.TestMicrosoftService(async () => {
                    await adapter.TestMicrosoftCalendarService();
                    await adapter.TestMicrosoftTodoService();
                });
            });
        }
#endif

		[Apartment(ApartmentState.STA)]
		[XpandTest(LongTimeout, 3)]
		[Test]
		[Ignore("Critical read requests")]
		public async Task Win_GoogleCloud_EasyTest() {
			DeleteBrowserFiles.Execute();
			await EasyTest(() => new WinAdapter(), RunWinApplication, async adapter => {
				await adapter.TestGoogleService(async () => {
					await adapter.TestGoogleCalendarService();
					await adapter.TestGoogleTasksService();
				});
			});
		}

		public static readonly string ApplicationPath =
			$@"{AppDomain.CurrentDomain.ApplicationPath()}\..\..\TestWinDesktopApplication\TestApplication.WinDesktop.exe";

		private TestApplication RunWinApplication(WinAdapter adapter, string connectionString) {
#if !NETCOREAPP3_1_OR_GREATER
            var fileName =
                Path.GetFullPath(
                    $@"{AppDomain.CurrentDomain.ApplicationPath()}\..\TestWinApplication\TestApplication.Win.exe");
#else
			var fileName = Path.GetFullPath(ApplicationPath);
#endif
			foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(fileName))) {
				process.Kill();
			}

			LogPaths.Clear();
			LogPaths.Add(Path.Combine(Path.GetDirectoryName(fileName)!, "eXpressAppFramework.log"));
			LogPaths.Add(Path.Combine(Path.GetDirectoryName(fileName)!,
				Path.GetFileName(ReactiveLoggerService.RXLoggerLogPath)!));
			return adapter.RunWinApplication(fileName, connectionString);
		}


		[Test]
		// [XpandTest(LongTimeout, 3)]
		// [Apartment(ApartmentState.STA)]
		public async Task Win_EasyTest_InLocalDb() {
			var connectionString =
				$"Integrated Security=SSPI;Pooling=false;Data Source=(localdb)\\mssqllocaldb;Initial Catalog=TestApplicationWin{AppDomain.CurrentDomain.UseNetFramework()}";
			await EasyTest(() => new WinAdapter(), RunWinApplication, adapter => {
				adapter.TestSequenceGeneratorService();
				return Task.CompletedTask;
			}, connectionString);
		}

		[Test()]
		[TestCaseSource(nameof(AgnosticModules))]
		[TestCaseSource(nameof(WinModules))]
		[XpandTest]
		public void UnloadWinModules(Type moduleType) {
			if (moduleType.Name==nameof(ReactiveLoggerModule)||moduleType.Name==nameof(ReactiveModule)) {
				return;
			}
			ReactiveModuleBase.Unload(moduleType);
			using var application = new TestWinApplication(moduleType, false);
			application.AddModule((ModuleBase)moduleType.CreateInstance(), nameof(UnloadWinModules));

			application.Modules.FirstOrDefault(m => m.GetType() == moduleType).ShouldBeNull();
		}

		[Test]
		[XpandTest(LongTimeout, 3)]
		public async Task Win_EasyTest_InMemory() {
			await EasyTest(() => new WinAdapter(), RunWinApplication, async adapter => {
				// var autoTestCommand = new AutoTestCommand("Event|Task|Reports");
				// adapter.Execute(autoTestCommand);
				adapter.TestBulkObjectUpdate();
				await adapter.TestEmail(ApplicationPath);
#if !XAF191 && !NETCOREAPP3_1_OR_GREATER
                adapter.TestDocumentStyleManager();
#endif
				adapter.TestModelViewInheritance();
				adapter.TestPositionInListView();
				await Task.CompletedTask;
			});
		}
	}
}