using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using DevExpress.ExpressApp;
using Xpand.Extensions.ProcessExtensions;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Windows {
    static class MultiInstanceService {
        public static IObservable<Window> MultiInstance(this IObservable<Window> source)
            => source.ConcatIgnored(frame => Process.GetCurrentProcess().WhenSameNameProcesses()
                .Do(process => {
                    var modelWindowsMultiInstance = frame.Model().MultiInstance;
                    if (!modelWindowsMultiInstance.Disabled) return;
                    if (!string.IsNullOrEmpty(modelWindowsMultiInstance.NotifyMessage)) {
                        throw new WarningException(
                            string.Format(modelWindowsMultiInstance.NotifyMessage, frame.Application.Title));
                    }
                    if (modelWindowsMultiInstance.FocusRunning&&process.GetUIThread()!=null) {
                        process.WaitForInputIdle();
                        SetForegroundWindow(process.MainWindowHandle);
                    }

                    frame.Application.Exit();
                }));

        private static IObservable<Process> WhenSameNameProcesses(this Process currentProcess) {
            var processesByName = Process.GetProcessesByName(currentProcess.ProcessName);
            return processesByName.Length == 1 ? Observable.Empty<Process>() : currentProcess.ReturnObservable()
                .TraceWindows(_ => processesByName.Length.ToString());
        }

        [DllImport("USER32.DLL")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}