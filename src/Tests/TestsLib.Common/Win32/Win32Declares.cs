using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Xpand.TestsLib.Common.Win32 {
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Win32Declares {
        public class COM {
#pragma warning disable 612,618
#pragma warning restore 612,618

#pragma warning disable 612,618
#pragma warning restore 612,618

        }

        [DllImport("user32")]
        public static extern int ToAscii(int uVirtKey, int uScanCode, byte[] lpbKeyState, byte[] lpwTransKey, int fuState);

        public class Menu {
            [DllImport("user32.dll")]
            public static extern int GetMenuItemID(IntPtr hMenu, int nPos);

            [DllImport("user32.dll")]
            public static extern IntPtr GetMenu(IntPtr hwnd);

            [DllImport("user32.dll")]
            public static extern IntPtr GetSubMenu(IntPtr hMenu, int nPos);
        }

        public class Message {
            public const int EM_SETSEL = 0x00B1;
            #region PostMessage

            [DllImport("user32.dll")]
            public static extern int PostMessage(IntPtr hwnd, uint wMsg, int wParam, int lParam);

            [DllImport("user32.dll")]
            public static extern void PostMessage(IntPtr handle, Win32Constants.KeyBoard keydown, Win32Constants.VirtualKeys keys, IntPtr intPtr);

            [DllImport("user32.dll")]
            public static extern int PostMessage(IntPtr hwnd, Win32Constants.Standard wMsg, int wParam, int lParam);

            [DllImport("user32.dll")]
            public static extern int PostMessage(IntPtr hwnd, Win32Constants.Button wMsg, IntPtr wParam, IntPtr lParam);
            #endregion
            #region SendMessage
            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern IntPtr SendMessageTimeout(IntPtr hWnd, Win32Constants.Standard msg, Win32Constants.Standard wParam, int lParam, Win32Constants.SendMessageTimeoutFlags fuFlags, uint uTimeout, out int lpdwResult);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern int SendMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr SendMessage(IntPtr hWnd, Win32Constants.Standard msg, IntPtr wParam, string s);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr SendMessage(IntPtr hWnd, Win32Constants.Standard msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr SendMessage(IntPtr hWnd, Win32Constants.Standard msg, IntPtr wParam, [Out] StringBuilder stringBuilder);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr SendMessage(IntPtr hWnd, Win32Constants.ListBox msg, IntPtr wParam, [Out] StringBuilder stringBuilder);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr SendMessage(IntPtr hWnd, Win32Constants.ListBox msg, IntPtr wParam, string s);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr SendMessage(IntPtr hWnd, Win32Constants.ListBox msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr SendMessage(IntPtr hWnd, Win32Constants.Button msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr SendMessage(IntPtr hWnd, Win32Constants.Focus msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr SendMessage(IntPtr hWnd, Win32Constants.Clipboard msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool SendNotifyMessage(Win32Constants.BroadCast broadCast, Win32Constants.BroadCastMessages broadCastMessages, UIntPtr wParam,
                                                        StringBuilder lParam);
            #endregion

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern void SendMessage(IntPtr ptr, Win32Constants.KeyBoard keyState, Win32Constants.VirtualKeys virtualKeys, IntPtr intPtr);


        }

        public class KeyBoard {
            [DllImport("user32.dll")]
            public static extern bool DestroyCaret();
            [DllImport("user32.dll", SetLastError=true)]
            public static extern bool HideCaret(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern uint SendInput(uint nInputs, Win32Types.INPUT[] pInputs, int cbSize);

            [DllImport("user32.dll")]
            public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

            [DllImport("user32")]
            public static extern int GetKeyboardState(byte[] pbKeyState);

            [DllImport("user32.dll")]
            public static extern int GetAsyncKeyState(int vKey);

            [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
            public static extern short GetKeyState(int keyCode);
        }

        public class Hooks {
            public delegate int KeyBoardCatcherDelegate(int code, int wparam, ref Win32Types.keybHookStruct lparam);

            public delegate int HookProc(int nCode, Int32 wParam, IntPtr lParam);

            [DllImport("user32", EntryPoint = "SetWindowsHookExA")]
            public static extern int SetWindowsHookEx(int idHook, KeyBoardCatcherDelegate lpfn, int hmod, int dwThreadId);


            [DllImport("user32", EntryPoint = "UnhookWindowsHookEx")]
            public static extern int UnhookWindowsHookEx(int hHook);

            [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public static extern int CallNextHookEx(int idHook, int nCode, Int32 wParam, IntPtr lParam);

            [DllImport("user32", EntryPoint = "CallNextHookEx")]
            public static extern int CallNextKeyboardHook(int hHook, int ncode, int wParam, Win32Types.keybHookStruct lParam);
        }

        public class Window{
            [DllImport("USER32.DLL")]
            public static extern int GetParent(int hWnd);
            public enum ShowScrollBarEnum{
                SB_HORZ = 0,
                SB_VERT = 1,
                SB_CTL = 2,
                SB_BOTH = 3
            }
            
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool ShowScrollBar(IntPtr hWnd, ShowScrollBarEnum wBar, [MarshalAs(UnmanagedType.Bool)] bool bShow);

            [DllImport("user32.dll")]
            public static extern bool IsIconic(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern bool DestroyWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern bool IsWindowEnabled(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

            [DllImport("user32.dll")]
            public static extern bool SetWindowText(IntPtr hWnd, StringBuilder stringBuilder);

            [DllImport("user32.dll")]
            public static extern IntPtr ChildWindowFromPointEx(IntPtr hWndParent, Win32Types.POINT pt, Win32Constants.ChildWindowFromPointFlags childWindowFromPointFlags);

            [DllImport("user32.dll")]
            public static extern IntPtr WindowFromPoint(Win32Types.POINT point);

            [DllImport("user32.dll")]
            public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);
            #region Get/SET WindowPlacement
            public enum WINDOWPLACEMENTFLAGS {
                WPF_SETMINPOSITION = 0x1,
                WPF_RESTORETOMAXIMIZED = 0x2

            }


            [DllImport("user32.dll")]
            public static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref Win32Types.WINDOWPLACEMENT lpwndpl);

            [DllImport("user32.dll")]
            public static extern bool GetWindowPlacement(IntPtr hWnd, out Win32Types.WINDOWPLACEMENT lpwndpl);
            #endregion

            [DllImport("user32.dll")]
            public static extern int GetWindowTextLength(IntPtr hwnd);

            [DllImport("user32.dll")]
            public static extern int GetWindowText(IntPtr hwnd, StringBuilder stringBuilder, int cch);

            public delegate int WindowEnumProc(IntPtr hwnd, IntPtr lparam);

            [DllImport("user32.dll")]
            public static extern bool EnumChildWindows(IntPtr hwnd, WindowEnumProc func, IntPtr lParam);
            #region SetWindowPos

            [DllImport("user32.dll")]
            public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, Win32Constants.SetWindowPosEnum setWindowPosEnum);
            #endregion
            #region SHowWindow
            public enum ShowWindowEnum {
                SW_HIDE = 0,

                SW_SHOWNORMAL = 1,

                SW_NORMAL = 1,

                SW_SHOWMINIMIZED = 2,

                SW_SHOWMAXIMIZED = 3,

                SW_MAXIMIZE = 3,

                SW_SHOWNOACTIVATE = 4,

                SW_SHOW = 5,

                SW_MINIMIZE = 6,

                SW_SHOWMINNOACTIVE = 7,

                SW_SHOWNA = 8,

                SW_RESTORE = 9,

                SW_SHOWDEFAULT = 10,

                SW_FORCEMINIMIZE = 11,

                SW_MAX = 11
            }

            [DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum showWindowEnum);
            [DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
            #endregion
        }

        public class WindowHandles {
            [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
            public static extern IntPtr FindWindowByCaption(IntPtr zeroOnly, string lpWindowName);

            [DllImport("user32.dll")]
            public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

            [DllImport("user32.dll")]
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

            [DllImport("user32.dll")]
            public static extern IntPtr GetDesktopWindow();

            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowDC(IntPtr hWnd);
        }

        public class WindowFocus {
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool BringWindowToTop(IntPtr hWnd);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern int SetFocus(int hwnd);

            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            public static extern IntPtr GetFocus();

        }

        public class Process {
            [DllImport("user32.dll",SetLastError = true)]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

            [DllImport("kernel32.dll")]
            public static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);
        }

        public class Thread {
            [DllImport("kernel32.dll")]
            public static extern uint GetCurrentThreadId();

            [DllImport("kernel32.dll")]
            public static extern IntPtr GetCurrentThread();

            [DllImport("user32.dll",SetLastError = true)]
            public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        }

        public class Rect {
            [DllImport("user32.dll",SetLastError = true)]
            public static extern bool GetWindowRect(IntPtr hWnd, out Win32Types.RECT lpRect);
        }

        public class MouseCursor {
            [DllImport("user32.dll")]
            public static extern bool DrawIcon(IntPtr hDC, int x, int y, IntPtr hIcon);
            [StructLayout(LayoutKind.Sequential)]
            public struct CURSORINFO {
                public Int32 cbSize;
                public readonly Int32 flags;
                public readonly IntPtr hCursor;
                public POINTAPI ptScreenPos;
            }

            [DllImport("user32.dll")]
            public static extern bool GetCursorInfo(out CURSORINFO pci);

            public const Int32 CURSOR_SHOWING = 0x00000001;

            [StructLayout(LayoutKind.Sequential)]
            public struct POINTAPI {
                public readonly int x;
                public readonly int y;
            }

        }

        public class Printers {
            [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool SetDefaultPrinter(string name);

            [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool GetDefaultPrinter(StringBuilder pszBuffer, ref int size);
        }

        public class Display {
            [DllImport("user32.dll")]
            public static extern Win32Constants.ChangeDisplaySettingsResult ChangeDisplaySettings(ref Win32Types.DEVMODE devMode, Win32Constants.ChangeDisplaySettingsFlags flags);

            [DllImport("user32.dll")]
            public static extern int EnumDisplaySettings(string deviceName, Win32Constants.EnumDisplaySettings modeNum, ref Win32Types.DEVMODE devMode);

        }

        public class GDI32 {
            [DllImport("gdi32.dll", SetLastError = true)]
            public static extern bool DeleteObject(IntPtr hObject);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

            [DllImport("gdi32.dll", SetLastError = true)]
            public static extern bool DeleteDC(IntPtr hDC);

            [DllImport("gdi32.dll", SetLastError = true)]
            public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

            [DllImport("gdi32.dll", SetLastError = true)]
            public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

            [DllImport("gdi32.dll")]
            public static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth,
                                             int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

            [DllImport("gdi32.dll")]
            public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth,
                                             int nHeight, IntPtr hObjSource, int nXSrc, int nYSrc, Win32Constants.TernaryRasterOperations dwRop);

        }

        public class IniFiles {
            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern Int32 GetPrivateProfileInt(
                String lpAppName,
                String lpKeyName,
                Int32 nDefault,
                String lpFileName);

            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern Int32 GetPrivateProfileSection(
                String lpAppName,
                Byte[] lpReturnedString,
                Int32 nSize,
                String lpFileName);

            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern Int32 GetPrivateProfileSectionNames(
                Byte[] lpszReturnBuffer,
                Int32 nSize,
                String lpFileName);

            [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern Int32 GetPrivateProfileString(
                String lpAppName,
                String lpKeyName,
                String lpDefault,
                StringBuilder lpReturnedString,
                Int32 nSize,
                String lpFileName);

            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern Int32 WritePrivateProfileSection(
                String lpAppName,
                String lpString,
                String lpFileName);

            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern Int32 WritePrivateProfileString(
                String lpAppName,
                String lpKeyName,
                String lpString,
                String lpFileName);

            [DllImport("kernel32.dll")]
            public static extern uint GetProfileString(string lpAppName, string lpKeyName,
                                                       string lpDefault, [Out] StringBuilder lpReturnedString, uint nSize);


        }
    }
}