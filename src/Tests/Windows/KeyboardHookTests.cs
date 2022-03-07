using System.Threading;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp.SystemModule;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.TestsLib.Common.InputSimulator;
using Xpand.TestsLib.Common.Win32;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Windows.Tests.BOModel;

namespace Xpand.XAF.Modules.Windows.Tests {
    public class KeyboardHookTests:BaseWindowsTest {

        [TestCase("S")]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public void MethodName(string shortcut) {
            using var application = WindowsModule().Application;
            var detailView = application.NewDetailView(typeof(W));
            detailView.CurrentObject = detailView.ObjectSpace.CreateObject<W>();
            var window = application.CreateViewWindow();
            window.SetView(detailView);
            var saveAction = window.GetController<ModificationsController>().SaveAction;
            saveAction.Model.Shortcut = shortcut;
            var testObserver = saveAction.WhenExecuted().Test();

            // var virtualKeys = (Win32Constants.VirtualKeys)saveAction.Keys();
            var inputSimulator = new InputSimulator();
            inputSimulator.Keyboard.KeyPress(Win32Constants.VirtualKeys.S);
            
            testObserver.AwaitDone(Timeout).ItemCount.ShouldBe(1);
        }

    }
}