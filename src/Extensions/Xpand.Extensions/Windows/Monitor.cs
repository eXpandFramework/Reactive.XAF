using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using static Xpand.Extensions.Windows.WinInterop;

namespace Xpand.Extensions.Windows;
public static class Monitor{
    public static void MoveActiveWindowToMainMonitor() 
        => PrimaryMonitor.MonitorBounds().ToRectangle()
            .MoveActiveWindowToMainMonitor(ptr => ptr.MonitorFromWindow().Equals(PrimaryMonitor));

    public static void MoveActiveWindowToMainMonitor(this Rectangle mainMonitor,Func<IntPtr,bool> primaryScreen){
        var hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero||primaryScreen(hWnd)) return;
        MoveActiveWindowToMainMonitor(mainMonitor, hWnd);
    }

    public static void MoveActiveWindowToMainMonitor(this Rectangle mainMonitor, IntPtr hWnd){
        ShowWindow(hWnd, SwRestore);
        var mainMonitorBounds = mainMonitor;
        SetWindowPos(hWnd, IntPtr.Zero, mainMonitorBounds.Left, mainMonitorBounds.Top, 0, 0, SwpNozorder);
        SendMessage(hWnd, WmPaint, IntPtr.Zero, IntPtr.Zero);
        UpdateWindow(hWnd);
        SetForegroundWindow(hWnd);
        ShowWindow(hWnd, SwMaximize);
        SendMessage(hWnd, WmPaint, IntPtr.Zero, IntPtr.Zero);
        UpdateWindow(hWnd);
    }
    
    public static IEnumerable<IntPtr> Monitors{
        get{
            var handles = new List<IntPtr>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr _, ref Rect _, IntPtr _)
                => {
                handles.Add(hMonitor);
                return true;
            }, IntPtr.Zero);
            return handles;
        }
    }
    
    public static RECT MonitorBounds(this IntPtr monitorHandle){
        var monitorInfo = new Monitorinfo{
            cbSize = (uint)Marshal.SizeOf(typeof(Monitorinfo))
        };
        GetMonitorInfo(monitorHandle, ref monitorInfo);
        return monitorInfo.rcMonitor;
    }
    
    public static IntPtr MonitorFromWindow(this IntPtr hwnd) 
        => WinInterop.MonitorFromWindow(hwnd, MonitorDefaultToNearest);
    
    public static void Move(this IntPtr intPtr, int x, int y,int width,int height) => MoveWindow(intPtr, x, y, width, height, true);
    
    public static bool UseInactiveMonitorBounds(this IntPtr handle, Action<RECT> bounds){
        var monitors = Monitors.ToArray();
        if (monitors.Length <= 1) return false;
        var currentScreen = handle.MonitorFromWindow();
        var inactiveScreen = monitors.FirstOrDefault(monitor => !Equals(monitor,currentScreen));
        if (inactiveScreen == IntPtr.Zero) return false;
        bounds(inactiveScreen.MonitorBounds());
        return true;
    }
    
    public static IntPtr PrimaryMonitor 
        => Monitors.FirstOrDefault(monitorHandle => {
            var monitorInfo = new Monitorinfo{ cbSize = (uint)Marshal.SizeOf(typeof(Monitorinfo)) };
            GetMonitorInfo(monitorHandle, ref monitorInfo);
            return monitorInfo.rcMonitor is{ Left: 0, Top: 0 };
        });

}

