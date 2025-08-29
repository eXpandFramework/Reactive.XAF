using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Xpand.XAF.Modules.Windows.SystemActions {
    internal static class Win32 {
        internal delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int RegisterHotKey(IntPtr hwnd, int id, int modifiers, int key);

        [DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int UnregisterHotKey(IntPtr hwnd, int id);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall,
            SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall,
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern int UnhookWindowsHookEx(IntPtr idHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall,
            SetLastError = true)]
        internal static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        internal static extern void keybd_event(byte key, byte scan, int flags, int extraInfo);

        internal static IntPtr SetWindowsHook(int hookType, HookProc callback) {
            IntPtr hookId;
            using (var currentProcess = Process.GetCurrentProcess())
            using (var currentModule = currentProcess.MainModule) {
                var handle = Win32.GetModuleHandle(currentModule!.ModuleName);
                hookId = Win32.SetWindowsHookEx(hookType, callback, handle, 0);
            }

            return hookId;
        }
    }
}