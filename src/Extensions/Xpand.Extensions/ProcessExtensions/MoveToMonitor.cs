using System;
using System.Diagnostics;
using Xpand.Extensions.Windows;
using static Xpand.Extensions.Windows.Monitor;
using static Xpand.Extensions.Windows.WinInterop;

namespace Xpand.Extensions.ProcessExtensions {
    public static partial class ProcessExtensions {
        private static void MoveTo<T>(this T process, WindowPosition position, RECT rect) where T : Process{
            for (var i = 0; i < 2; i++){
                GetWindowRect(process.MainWindowHandle, out var currentRect);
                var currentWidth = currentRect.Right - currentRect.Left;
                var currentHeight = currentRect.Bottom - currentRect.Top;

                if (position.HasFlag(WindowPosition.Small)){
                    currentHeight /= 2;
                }
                
                position &= ~WindowPosition.Small;
                var (x, y, width, height) = position switch{
                    WindowPosition.FullScreen => (rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top),
                    WindowPosition.BottomRight => (rect.Right - currentWidth, rect.Bottom - currentHeight, currentWidth, currentHeight),
                    WindowPosition.BottomLeft => (rect.Left, rect.Bottom - currentHeight, currentWidth, currentHeight),
                    _ => throw new ArgumentOutOfRangeException(nameof(position), position, null)
                };
                process.MainWindowHandle.Move(x, y, width, height);
            }
        }

        public static T MoveToMonitor<T>(this T process, WindowPosition position = WindowPosition.None,
            bool alwaysOnTop = false) where T : Process {
            if (position != WindowPosition.None) {
                if (!process.MainWindowHandle.UseInactiveMonitorBounds(rect => process.MoveTo(position, rect))) {
                    process.MoveTo(position, PrimaryMonitor.MonitorBounds());
                }
            }

            process.MainWindowHandle.AlwaysOnTop(alwaysOnTop);
            return process;
        }
    }
}