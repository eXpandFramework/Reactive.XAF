using System.Diagnostics;
using System.Linq;
using System.Threading;
using Xpand.TestsLib.Common.Win32;

namespace Xpand.TestsLib.Common.EasyTest.Commands.Automation{
    public class DeleteBrowserFiles{
        public static void Execute(){
            KillBrowser();
            Process.Start("control", "inetcpl.cpl");
            Thread.Sleep(1400);
            var keyboard = new Common.InputSimulator.InputSimulator().Keyboard;
            var millisecondsTimeout = 1000;
            keyboard.ModifiedKeyStroke(Win32Constants.VirtualKeys.AltLeft, Win32Constants.VirtualKeys.D);
            Thread.Sleep(millisecondsTimeout);
            keyboard.ModifiedKeyStroke(Win32Constants.VirtualKeys.AltLeft, Win32Constants.VirtualKeys.D);
            Thread.Sleep(millisecondsTimeout);
            keyboard.ModifiedKeyStroke(Win32Constants.VirtualKeys.AltLeft, Win32Constants.VirtualKeys.A);
            keyboard.KeyPress(Win32Constants.VirtualKeys.Escape);
            Thread.Sleep(3000);
        }

        private static void KillBrowser(){
            foreach (var p in Process.GetProcessesByName("iexplore").Where(process => !process.HasExited)){
                try{
                    p.Kill();
                }
                catch{
                    // ignored
                }
            }
        }
    }
}