using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Windows.Tests {
    [NonParallelizable]
    public class NotifyIconTests : BaseWindowsTest {
        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public async Task Enable() {
            var updated = NotifyIconService.NotifyIconUpdated.FirstAsync().SubscribeReplay();
            using var application = Platform.Win.NewApplication<WindowsModule>();
            application.SetupSecurity();
            application.AddModule<WindowsModule>();
            
            var modelWindows = application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows;
            modelWindows.NotifyIcon.Enabled = true;
            application.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(), true);
            

            application.Logon();

            await updated;
        }

        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public async Task ExitApplication() {
            using var application = Platform.Win.NewApplication<WindowsModule>();
            application.SetupSecurity();
            application.AddModule<WindowsModule>();
            
            var modelWindows = application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows;
            modelWindows.NotifyIcon.Enabled = true;
            application.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(), true);
            var updated = NotifyIconService.NotifyIconUpdated
                .Do(icon => icon.ContextMenuStrip.Items.Cast<ToolStripMenuItem>()
                    .First(item => item.Text == modelWindows.NotifyIcon.ExitText).PerformClick())
                .FirstAsync().SubscribeReplay();
            var whenExiting = application.WhenExiting().FirstAsync().SubscribeReplay();
            application.Logon();
            await updated;
            await whenExiting;
        
            await Task.Delay(TimeSpan.FromSeconds(3));
        
        }


        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public async Task LogOffApplication() {
            using var application = Platform.Win.NewApplication<WindowsModule>();
            application.SetupSecurity();
            application.AddModule<WindowsModule>();
            
            var modelWindows = application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows;
            modelWindows.NotifyIcon.Enabled = true;
            application.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(), true);
            var updated = NotifyIconService.NotifyIconUpdated
                .Do(icon => icon.ContextMenuStrip.Items.Cast<ToolStripMenuItem>()
                    .First(item => item.Text == modelWindows.NotifyIcon.LogOffText).PerformClick())
                .FirstAsync().SubscribeReplay();
            application.Logon();
            var testObserver = application.WhenLoggingOff().Do(t => t.e.Cancel=true).FirstAsync().SubscribeReplay();
            await updated;

            await testObserver;

        }

        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public async Task HideApplication() {
            
            using var application = Platform.Win.NewApplication<WindowsModule>();
            application.SetupSecurity();
            application.AddModule<WindowsModule>();
            
            var modelWindows = application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows;
            modelWindows.NotifyIcon.Enabled = true;
            application.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(), true);
            var updated = NotifyIconService.NotifyIconUpdated
                .Do(icon => icon.ContextMenuStrip.Items.Cast<ToolStripMenuItem>()
                    .First(item => item.Text == modelWindows.NotifyIcon.HideText).PerformClick())
                .FirstAsync().SubscribeReplay();
            application.Logon();

            var visibleChanged = application.MainWindow.Template.WhenEvent(nameof(Form.VisibleChanged)).FirstAsync()
                .SubscribeReplay();
                
            await updated;
            
            await visibleChanged;

        }

    }
}