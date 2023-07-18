using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Forms;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Windows.Tests.BOModel;

namespace Xpand.XAF.Modules.Windows.Tests {
	public class FormNodeTests:BaseWindowsTest {
		[TestCase(true)]
		[TestCase(false)]
		[XpandTest]
		[Apartment(ApartmentState.STA)]
		public  void Disable_ControlBox(bool popup){
			using var application = WindowsModule().Application;
			var modelWindows = application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows;
			modelWindows.Form.ControlBox = false;
            modelWindows.Form.PopupWindows = popup;
			var visibility = application.WhenWindowCreated(true)
                .Select(window => {
                    if (window.Context == TemplateContext.ApplicationWindow&&popup) {
                        var popupWindow = application.CreatePopupWindow(TemplateContext.PopupWindow,
                            window.Application.FindListViewId(typeof(W)));
                        popupWindow.SetView(application.NewListView(typeof(W)));
                        return popupWindow;
                    }

                    return window;
                })
				.TakeFirst()
				.Select(window => ((Form) window.Template).ControlBox)
                .Do(_ => application.Exit())
                .SubscribeReplay();

			((TestWinApplication)application).Start();

			visibility.Test().Items.First().ShouldBeFalse();
        }

		[Test]
		[XpandTest]
		[Apartment(ApartmentState.STA)]
		public  void Disable_Minimize(){
			using var application = WindowsModule().Application;
			var modelWindows = application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows;
			modelWindows.Form.MinimizeBox = false;
			var visibility = application.WhenWindowCreated(true)
				.TakeFirst()
				.Select(_ => ((Form) application.MainWindow.Template).MinimizeBox)
				.Do(_ => application.Exit())
				.SubscribeReplay();

			((TestWinApplication)application).Start();

			visibility.Test().Items.First().ShouldBeFalse();
        }

		[Test]
		[XpandTest]
		[Apartment(ApartmentState.STA)]
		public  void Disable_Maximize(){
			using var application = WindowsModule().Application;
			var modelWindows = application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows;
			modelWindows.Form.MaximizeBox = false;
			var visibility = application.WhenWindowCreated(true)
				.TakeFirst()
				.Select(_ => ((Form) application.MainWindow.Template).MaximizeBox)
				.Do(_ => application.Exit())
				.SubscribeReplay();

			((TestWinApplication)application).Start();

			visibility.Test().Items.First().ShouldBeFalse();
        }
	
	}
}