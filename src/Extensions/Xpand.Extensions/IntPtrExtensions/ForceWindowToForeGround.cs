using System;
using System.Runtime.InteropServices;

namespace Xpand.Extensions.IntPtrExtensions {
    public static partial class IntPtrExtensions {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);
        [DllImport("user32.dll")]
        static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        public static void ForceWindowToForeGround(this IntPtr handle) {
            var activeWindowHandle = GetForegroundWindow();
            var activeWindowThread = GetWindowThreadProcessId(activeWindowHandle, IntPtr.Zero);
            var thisWindowThread = GetWindowThreadProcessId(handle, IntPtr.Zero);
            AttachThreadInput(activeWindowThread, thisWindowThread, true);
            AttachThreadInput(activeWindowThread, thisWindowThread, false);
        }
    }
}