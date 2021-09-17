using System;
using System.Diagnostics;
using System.Linq;

namespace Xpand.Extensions.ProcessExtensions {
    public static partial class ProcessExtensions {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, IntPtr procId);
        public static ProcessThread GetUIThread(this Process proc) {
            int id = GetWindowThreadProcessId(proc.MainWindowHandle, IntPtr.Zero);
            return proc.Threads.Cast<ProcessThread>().FirstOrDefault(pt => pt.Id == id);
        }
    }
}