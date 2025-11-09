using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Workflow.Tests.BOModel;

namespace Xpand.XAF.Modules.Workflow.Tests.Common {
	public abstract class BaseWorkflowTest:BaseTest {
		protected XafApplication NewApplication() => Platform.Win.NewApplication<ReactiveModule>(mockEditors:false);
		protected virtual WorkflowModule WorkflowModule(XafApplication application){
			application.AddModule<TestWorkflowModule>(typeof(WF),typeof(TestCommand));
			return application.Modules.FindModule<WorkflowModule>();
		}
	}

	static class TestWindowsService {
		public static SimpleAction WindowsAction(this (WorkflowModule, Frame frame) tuple) 
			=> tuple.frame.Action(nameof(WindowsAction)).As<SimpleAction>();
		public static SimpleAction ViewAction(this (WorkflowModule, Frame frame) tuple) 
			=> tuple.frame.Action(nameof(ViewAction)).As<SimpleAction>();

		public static IObservable<Unit> TestWindowsConnect(this ApplicationModulesManager manager) 
			=> manager.RegisterWindowSimpleAction(nameof(WindowsAction)).ToUnit()
				.Merge(manager.RegisterViewSimpleAction(nameof(ViewAction)).ToUnit());
	}
}