using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Layout;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Web;
using DevExpress.ExpressApp.Web.Layout;
using DevExpress.ExpressApp.Win;
using JetBrains.Annotations;
using Moq;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Logger;
using Xpand.XAF.Modules.Reactive.Logger.Hub;

// ReSharper disable once CheckNamespace
namespace Xpand.TestsLib {
	public class TestWinApplication : WinApplication, ITestApplication {
		private readonly bool _transmitMessage;

		[SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
		public TestWinApplication(Type sutModule, bool transmitMessage = true, bool handleExceptions = true) {
			// SettingUp += (_, args) => ((ExportedTypeCollection)args.SetupParameters.DomainComponents).Add(typeof(TraceEvent));
			_transmitMessage = transmitMessage;

			SUTModule = sutModule;

			CustomHandleException += (_, e) => {
				if (handleExceptions) {
					throw e.Exception;
				}
			};
			TraceClientConnected = this.ClientConnect();
			TraceClientBroadcast = this.ClientBroadcast();
		}

		public bool TransmitMessage => _transmitMessage;

		public IObservable<Unit> TraceClientBroadcast { get; set; }


		public IObservable<Unit> TraceClientConnected { get; set; }

		public Type SUTModule { get; }

		protected override void Dispose(bool disposing) {
			if (_transmitMessage) {
				// TraceClientConnected.ToTaskWithoutConfigureAwait().GetAwaiter().GetResult();
				// TraceClientBroadcast.ToTaskWithoutConfigureAwait().GetAwaiter().GetResult();
			}

			base.Dispose(disposing);
		}

		readonly Subject<Form> _modelEditorForm = new();

		public TestWinApplication() { }

		public IObservable<Form> ModelEditorForm => _modelEditorForm.AsObservable();

		protected override Form CreateModelEditorForm() {
			var modelEditorForm = base.CreateModelEditorForm();
			_modelEditorForm.OnNext(modelEditorForm);
			return modelEditorForm;
		}

//        protected override LayoutManager CreateLayoutManagerCore(bool simple){
//            if (!simple){
//                var controlMock = new Mock<Control>(){CallBase = true};
//                var layoutManagerMock = new Mock<WinLayoutManager>(){CallBase = true};
//                layoutManagerMock.Setup(_ => _.LayoutControls(It.IsAny<IModelNode>(), It.IsAny<ViewItemsCollection>())).Returns(controlMock.Object);
//            
//                return layoutManagerMock.Object;
//            }
//
//            return new WinSimpleLayoutManager();
//        }

		protected override string GetModelCacheFileLocationPath() {
			return null;
		}

		protected override string GetDcAssemblyFilePath() {
			return null;
		}

		public override void StartSplash() { }

		protected override string GetModelAssemblyFilePath() {
			return $@"{AppDomain.CurrentDomain.ApplicationPath()}\ModelAssembly{Guid.NewGuid()}.dll";
		}
	}

	public class TestWebApplication : WebApplication, ITestApplication {
		private readonly bool _transmitMessage;

		public TestWebApplication(Type sutModule, bool transmitMessage = true) {
			// SettingUp += (_, args) => ((ExportedTypeCollection)args.SetupParameters.DomainComponents).Add(typeof(TraceEvent));
			_transmitMessage = transmitMessage;
			SUTModule = sutModule;
			TraceClientConnected = this.ClientConnect();
			TraceClientBroadcast = this.ClientBroadcast();
			TransmitMessage = transmitMessage;
		}

		public bool TransmitMessage { get; }
		public IObservable<Unit> TraceClientBroadcast { get; set; }

		protected override bool CanLoadTypesInfo() {
			return true;
		}

		protected override void Dispose(bool disposing) {
			if (_transmitMessage) {
//                var timeout = TimeSpan.FromMilliseconds(5000);
//                TraceClientConnected.Timeout(timeout).Wait();
//                TraceClientBroadcast.Timeout(timeout).Wait();
			}

			base.Dispose(disposing);
		}

		protected override bool IsSharedModel => false;
		public IObservable<Unit> TraceClientConnected { get; set; }
		[PublicAPI] public Type SUTModule { get; set; }

		protected override LayoutManager CreateLayoutManagerCore(bool simple) {
			var controlMock = new Mock<Control>() { CallBase = true };
			var layoutManagerMock = new Mock<WebLayoutManager>() { CallBase = true };
			layoutManagerMock.Setup(_ => _.LayoutControls(It.IsAny<IModelNode>(), It.IsAny<ViewItemsCollection>()))
				.Returns(controlMock.Object);
			return layoutManagerMock.Object;
		}
	}

	[PublicAPI]
	static class TestApplicationExtensions {
		public static IObservable<Unit> ClientBroadcast(this ITestApplication application) {
			return Process.GetProcessesByName("Xpand.XAF.Modules.Reactive.Logger.Client.Win").Any()
				? TraceEventHub.Trace.FirstAsync(_ => _.Source == application.SUTModule.Name).ToUnit()
					.SubscribeReplay()
				: Unit.Default.ReturnObservable();
		}

		[PublicAPI]
		public static IObservable<Unit> ClientConnect(this ITestApplication application) {
			return Process.GetProcessesByName("Xpand.XAF.Modules.Reactive.Logger.Client.Win").Any()
				? TraceEventHub.Connecting.FirstAsync().SubscribeReplay()
				: Unit.Default.ReturnObservable();
		}
	}
}