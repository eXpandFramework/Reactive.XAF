using System;
using DevExpress.ExpressApp.Blazor;
using NUnit.Framework;

namespace Xpand.TestsLib.Blazor {
	public abstract class BlazorCommonAppTest : BlazorCommonTest {
		public BlazorApplication Application { get; set; }

		public override void Dispose() { }

		protected override void ResetXAF() { }


		[OneTimeTearDown]
		public override void Cleanup() {
			base.Cleanup();
			Application?.Dispose();
			

			base.Dispose();
			CleanBlazorEnvironment();
		}

		[OneTimeSetUp]
		public override void Init() {
			base.Init();
			Application = NewBlazorApplication(StartupType);
		}

		protected abstract Type StartupType { get; }
	}
}