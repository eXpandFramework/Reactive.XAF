using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using Xpand.Extensions.Windows;
using static Xpand.Extensions.Windows.WinInterop;

namespace Xpand.TestsLib.Common{
    public static class ScreenCapture{
        public static Bitmap CaptureActiveWindow(){
            var hWnd = GetForegroundWindow();
            return hWnd != IntPtr.Zero ? DwmGetWindowAttribute(hWnd, DwmwaExtendedFrameBounds, out var rect, Marshal.SizeOf(typeof(RECT))) == 0
                ? rect.Capture() : null : null;
        }
        public static Bitmap Capture(this RECT rect) 
            => new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top).Capture();

        public static Bitmap Capture(this Rectangle bounds){
            var result = new Bitmap(bounds.Width, bounds.Height);
            using var g = Graphics.FromImage(result);
            g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
            return result;
        }

        
        public static string CaptureActiveWindowAndSave(string path=null){
            Monitor.MoveActiveWindowToMainMonitor();
            var filename = path??Path.GetTempFileName().Replace(".tmp", ".bmp");
            using var bitmap = CaptureActiveWindow();
            bitmap?.Save(filename);
            return new Uri(filename).AbsoluteUri;
        }
    }
    

}