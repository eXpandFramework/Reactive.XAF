using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Xpand.TestsLib;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Windows.Tests {
    public class WindowsTests : BaseWindowsTest {
        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        [TestCase(true)]
        [TestCase(false)]
        public  void StartupWithWindows(bool enable) {
            using var application = WindowsModule().Application;
            var modelWindows = application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows;
            modelWindows.Startup=enable;
            application.WhenWindowCreated(true).Do(_ => application.Exit()).Test();

            ((TestWinApplication) application).Start();

            string deskDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var path = deskDir + "\\" + application.Title + ".url";
            if (enable) {
                if (!File.Exists(path)) {
                    throw new FileNotFoundException(path);
                }
            }
            else {
                if (File.Exists(path)) {
                    throw new FileNotFoundException(path);
                }
            }

        }

    }
}