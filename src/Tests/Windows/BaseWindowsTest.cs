using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Windows.Tests.BOModel;

namespace Xpand.XAF.Modules.Windows.Tests {

	public abstract class BaseWindowsTest:BaseTest {
		protected WindowsModule WindowsModule(Platform platform=Platform.Win){
			var application = platform.NewApplication<WindowsModule>();
			application.WhenApplicationModulesManager().TakeFirst().SelectMany(manager => manager.TestWindowsConnect()).Subscribe();
			application.EditorFactory=new EditorsFactory();
			var oneViewModule = application.AddModule<WindowsModule>(typeof(W), typeof(W));
            
			return oneViewModule;
		}
	}

	static class TestWindowsService {
		public static SimpleAction WindowsAction(this (WindowsModule, Frame frame) tuple) 
			=> tuple.frame.Action(nameof(WindowsAction)).As<SimpleAction>();
		public static SimpleAction ViewAction(this (WindowsModule, Frame frame) tuple) 
			=> tuple.frame.Action(nameof(ViewAction)).As<SimpleAction>();

		public static IObservable<Unit> TestWindowsConnect(this ApplicationModulesManager manager) {
			return manager.RegisterWindowSimpleAction(nameof(WindowsAction)).ToUnit()
				.Merge(manager.RegisterViewSimpleAction(nameof(ViewAction)).ToUnit());
		}

	}
}