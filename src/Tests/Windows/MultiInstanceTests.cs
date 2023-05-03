using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using akarnokd.reactive_extensions;
using Fasterflect;
using HarmonyLib;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.StringExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Windows.Tests {
    public class MultiInstanceTests:BaseWindowsTest {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static bool GetProcessesByName(ref Process[] __result,string processName) {
            var currentProcess = Process.GetCurrentProcess();
            if (processName == currentProcess.ProcessName) {
                __result = new[] {currentProcess, currentProcess};
                return false;
            }

            return true;
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public  void Disable_With_Message(){
            var harmony = new Harmony(nameof(Disable_With_Message));
            var original = typeof(Process).Method(nameof(Process.GetProcessesByName),new[]{typeof(string)},Flags.StaticPublic);
            harmony.Patch(original, new HarmonyMethod(typeof(MultiInstanceTests), nameof(GetProcessesByName)));
            using var application = WindowsModule().Application;

            var modelWindows = application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows;
            modelWindows.MultiInstance.Disabled=true;

            var exception = Should.Throw<Exception>(() => ((TestWinApplication) application).Start());

            exception.Message.ShouldBe(modelWindows.MultiInstance.NotifyMessage.StringFormat(application.Title));

            harmony.UnpatchAll(nameof(Disable_With_Message));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public  void Exit_Silent(){
            var harmony = new Harmony(nameof(Exit_Silent));
            var original = typeof(Process).Method(nameof(Process.GetProcessesByName),new[]{typeof(string)},Flags.StaticPublic);
            harmony.Patch(original, new HarmonyMethod(typeof(MultiInstanceTests), nameof(GetProcessesByName)));

            using var application = WindowsModule().Application;
            var exiting = application.WhenExiting().Test();
            var modelWindows = application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows;
            modelWindows.MultiInstance.Disabled=true;
            modelWindows.MultiInstance.NotifyMessage=null;
            modelWindows.MultiInstance.FocusRunning = false;

            ((TestWinApplication) application).Start();

            exiting.ItemCount.ShouldBe(1);

            harmony.UnpatchAll(nameof(Exit_Silent));
        }

    }
}