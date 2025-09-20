using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reactive;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.Transform.System.Diagnostics{
    public static class ProcessEx{
        
        public static IObservable<float> PrivateBytes(this Process process,IObservable<Unit> signal) 
            => Observable.Using(() => new PerformanceCounter("Process", "Private Bytes", process.ProcessName),counter => signal
                .Select(_ => counter.NextValue()).Select(sample => sample));

        public static bool StartWithEvents(this Process process,bool outputDataReceived=true,bool outputErrorReceived=true,bool enableRaisingEvents=true,bool createNoWindow=true){
            process.StartInfo.RedirectStandardOutput = outputDataReceived;
            process.StartInfo.RedirectStandardError = outputErrorReceived;
            if (outputDataReceived||outputErrorReceived){
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = createNoWindow;
            }
            process.EnableRaisingEvents = enableRaisingEvents;
            var start = process.Start();
            if (start&&outputDataReceived){
                process.BeginOutputReadLine();    
            }
            if (start&&outputErrorReceived){
                process.BeginErrorReadLine();    
            }
            return start;
        }

        public static IObservable<string> WhenOutputDataReceived(this Process process)
            => process.ProcessEvent<DataReceivedEventArgs>(nameof(Process.OutputDataReceived))
                
                .Select(pattern => pattern.Data);

        public static IObservable<string> WhenErrorDataReceived(this Process process)
            => process.ProcessEvent<DataReceivedEventArgs>(nameof(Process.ErrorDataReceived))
                .Select(pattern => pattern.Data);

        public static IObservable<Process> WhenExited(this Process process) 
            => process.ProcessEvent(nameof(Process.Exited)).Take(1).To(process);

        public static IObservable<Process> WhenNewProcess(this ProcessStartInfo existing,bool systemManagement=false) 
            => WhenNewProcess(Path.GetFileNameWithoutExtension(existing.FileName),systemManagement);

        private static IObservable<Process> WhenNewProcess(string processName,bool systemManagement=false){
            var processes = Process.GetProcessesByName(processName).Select(process => process.Id).ToHashSet();
            return Observable.Using(
                () => new ManagementEventWatcher(
                    $"SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName = '{processName}.exe'"), watcher => {
                    watcher.Start();
                    return watcher.ProcessEvent<EventArrivedEventArgs>(nameof(watcher.EventArrived))
                        .Select(e => Convert.ToInt32((UInt32)e.NewEvent.Properties["ProcessID"].Value))
                        .Where(processId => !processes.Contains(processId))
                        .SelectMany(processId => Observable.Defer(() => Process.GetProcessById(processId).Observe())
                            .OnErrorResumeNext(Observable.Empty<Process>()).CompleteOnError())
                        .Select(eventArgs => eventArgs)
                        .Merge(watcher.WhenDisposed().Do(_ => watcher.Stop()).IgnoreElements().To<Process>())
                        .DoOnComplete(() => { });
                });
        }

        public static IObservable<Process> WhenNewProcess(this Process existing,bool systemManagement=false) 
            => WhenNewProcess(existing.ProcessName,systemManagement);
    }
}