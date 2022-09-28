using System.Collections.Generic;
using System.Linq;
using System.Threading;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Windows.SystemActions;
using Xpand.XAF.Modules.Windows.Tests.BOModel;

namespace Xpand.XAF.Modules.Windows.Tests {
    public class SystemActionsTests:BaseWindowsTest {

        
        [XpandTest]
        [Apartment(ApartmentState.STA)][Test]
        public void RegisterWindowsAction() {
            var observer = SystemActionsService.CustomizeHotKeyManager.Test();
            using var application = WindowsModule().Application;
            var windowsSystemActions = application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows.HotkeyActions;
            var modelSystemAction = windowsSystemActions.AddNode<IModelSystemAction>();
            modelSystemAction.Action = application.Model.ActionDesign.Actions[nameof(TestWindowsService.WindowsAction)];
            modelSystemAction.HotKey = "LWin + Control + Z";
            var window = application.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(), true);
            window.CreateTemplate();
            observer.ItemCount.ShouldBe(1);

            var hotKeyManager = observer.Items.First();
            hotKeyManager.GlobalHotKeyCount.ShouldBe(1);
            window.Close();
            hotKeyManager.GlobalHotKeyCount.ShouldBe(0);
        }
        
        [XpandTest]
        [Apartment(ApartmentState.STA)][Test]
        public void RegisterViewAction() {
            var observer = SystemActionsService.CustomizeHotKeyManager.Test();
            using var application = WindowsModule().Application;
            var windowsSystemActions = application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows.HotkeyActions;
            var modelSystemAction = windowsSystemActions.AddNode<IModelSystemAction>();
            modelSystemAction.Action = application.Model.ActionDesign.Actions[nameof(TestWindowsService.ViewAction)];
            var modelViewLink = modelSystemAction.Views.AddNode<IModelViewLink>();
            modelViewLink.View = application.Model.BOModel.GetClass(typeof(W)).DefaultListView;
            modelSystemAction.HotKey = "LWin + Control + Z";
            var window = application.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(), true);
            window.CreateTemplate();
            window.SetView(application.NewListView(typeof(W)));
            observer.ItemCount.ShouldBe(1);

            var hotKeyManager = observer.Items.First();
            hotKeyManager.GlobalHotKeyCount.ShouldBe(1);
            window.SetView(null);
            hotKeyManager.GlobalHotKeyCount.ShouldBe(0);
        }

    }
}