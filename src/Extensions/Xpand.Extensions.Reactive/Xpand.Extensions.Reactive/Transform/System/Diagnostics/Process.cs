using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;

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
            var start = process.Start();
            process.EnableRaisingEvents = enableRaisingEvents;
            if (start&&outputDataReceived){
                process.BeginOutputReadLine();    
            }
            if (start&&outputErrorReceived){
                process.BeginErrorReadLine();    
            }
            return start;
        }

        public static IObservable<string> WhenOutputDataReceived(this Process process) 
            => Observable.FromEventPattern<DataReceivedEventArgs>(process, nameof(Process.OutputDataReceived)).Select(ep => ep.EventArgs.Data);

        public static IObservable<string> WhenErrorDataReceived(this Process that)
            => Observable.FromEventPattern<DataReceivedEventArgs>(that, nameof(Process.ErrorDataReceived))
                .Select(ep => ep.EventArgs.Data);

        public static IObservable<Unit> WhenExited(this Process process) 
            => Observable.FromEventPattern<EventArgs>(process, nameof(Process.Exited)).Select(_ => Unit.Default);
    }
}