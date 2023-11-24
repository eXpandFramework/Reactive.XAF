using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.ProcessExtensions;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Windows;
using static Xpand.TestsLib.Common.Logger;

namespace Xpand.TestsLib.Common{

    public class Logger:Process{
        internal static readonly TextWriter OriginalOut = Console.Out;
        internal static readonly List<string> CachedMessages = new();
        public static async Task<StreamWriter> Writer(LogContext context=default,WindowPosition inactiveMonitorLocation=WindowPosition.None,bool alwaysOnTop=false) 
            => await new Logger().ConnectClient(context,inactiveMonitorLocation,alwaysOnTop).Writer().ReplayFirstTake();

        public  static void Exit(){
            try{
                Console.WriteLine(ExitSignal);
                Console.SetOut(OriginalOut);
                CachedMessages.Do(Console.WriteLine).Enumerate();
                
            }
            catch{
                // ignored
            }
        }

        internal const string ExitSignal = "exit";
        public string PipeName{ get; set; } = nameof(Logger);
        public string ServerName{ get; set; } = ".";
        public TimeSpan ConnectionTimeout{ get; set; } = 5.Seconds();
        public string PowerShellName{ get; set; } =Directory.Exists("C:\\Program Files\\PowerShell\\")?"pwsh.exe": "powershell.exe";
    }
    public static class LoggerExtensions{
        public static IObservable<NamedPipeClientStream> ConnectClient(this Logger logger,LogContext context=default,WindowPosition inactiveMonitorLocation=WindowPosition.None,bool alwaysOnTop=false){
            var startServer = logger.StartServer(context);
            return startServer.Connect().Do(_ => startServer.MoveToMonitor(inactiveMonitorLocation,alwaysOnTop));
        }

        private static Logger StartServer(this Logger logger,string condition=".*"){
            var script = $@"function Monitor-Pipe($condition) {{
                        $pipeName = '{logger.PipeName}'
                        $pipe = New-Object System.IO.Pipes.NamedPipeServerStream($pipeName)
                        $pipe.WaitForConnection()
                        $reader = New-Object System.IO.StreamReader($pipe)
                        $interactive=$false
                        while ($true) {{
                            $message = $reader.ReadLine()
                            Write-Host $message
                            if ($message -match $condition){{
                                $interactive=$true
                            }}
                            if ($message -ne '{ExitSignal}') {{
                                if ($interactive){{
                                    $null = $host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
                                }}
                            }}
                            else {{
                                $pipe.Dispose()
                                Start-Sleep -Seconds 5
                                Stop-Process -Id $PID
                            }}
                        }}
                    }}
                    Monitor-Pipe {condition}";
            logger.StartInfo.FileName = logger.PowerShellName;
            logger.StartInfo.Arguments = $"-NoExit -Command {script}";
            logger.StartInfo.UseShellExecute = true;
            AppDomain.CurrentDomain.KillAll(Path.GetFileNameWithoutExtension(logger.PowerShellName));
            logger.Start();
            return logger;
        }


        public static IObservable<StreamWriter> Writer(this IObservable<NamedPipeClientStream> source) 
            => source.Select(client => client.Writer());

        public static StreamWriter Writer(this NamedPipeClientStream client) => new(client){AutoFlush = true};

        private static IObservable<NamedPipeClientStream> Connect(this Logger logger) 
            => Observable.Interval(100.Milliseconds())
                .Select(_ => new NamedPipeClientStream(logger.ServerName, logger.PipeName, PipeDirection.Out))
                .SelectManySequential(clientStream => clientStream.ConnectAsync().ToObservable()
                    .Catch<Unit,TimeoutException>(_ => Observable.Empty<Unit>()).To(clientStream))
                .Timeout(logger.ConnectionTimeout).WhenNotDefault(stream => stream.IsConnected).Take(1);

        public static IObservable<T> Log<T>(this IObservable<T> source,Func<T,string> messageFactory,TextWriter textWriter=null,[CallerMemberName]string caller="") 
            => source.Do(x => $"{caller}: {messageFactory(x)}".LogValue(textWriter));
        
        public static IObservable<T> Log<T>(this IObservable<T> source,Func<T,string> messageFactory,[CallerMemberName]string caller="") 
            => source.Log(messageFactory,null,caller);
        
        public static T LogValue<T>(this T value,TextWriter textWriter=null){
            textWriter?.WriteLine(value);
            Console.WriteLine(value);
            return value;
        }

        public static IObservable<T> Log<T>(this IObservable<T> source, LogContext logContext,
            WindowPosition inactiveMonitorLocation = WindowPosition.None, bool alwaysOnTop = false) 
            => source.Publish(obs => logContext.Observe().If(context => context == default, _ => obs,
                context => Logger.Writer(context, inactiveMonitorLocation, alwaysOnTop).ToObservable()
                    .Do(writer => Console.SetOut(new InterceptingTextWriter(writer, CachedMessages)))
                    .IgnoreElements().DoNotComplete().To<T>()
                    .TakeUntilCompleted(obs)
                    .Merge(obs.DoOnError(_ => Exit())
                        .DoOnComplete(Exit))));

        class InterceptingTextWriter : TextWriter{
            private readonly TextWriter _originalWriter;
            private readonly List<string> _cachedMessages;

            public InterceptingTextWriter(TextWriter originalWriter, List<string> cachedMessages){
                _originalWriter = originalWriter;
                _cachedMessages = cachedMessages;
            }

            public override void WriteLine(string value){
                _cachedMessages.Add(value);
                _originalWriter.WriteLine(value);
            }
            
            public override Encoding Encoding => _originalWriter.Encoding;
        }

    }

    public readonly struct LogContext : IEquatable<LogContext>{
        public override bool Equals(object obj) => obj is LogContext other && Equals(other);
        
        public override int GetHashCode() => CustomValue != null ? CustomValue.GetHashCode() : 0;

        private LogContext(string customValue) => CustomValue = customValue;

        public static LogContext All => new("All");
        public static LogContext None => new("None");

        public bool Equals(LogContext other) => CustomValue == other.CustomValue;

        public static bool operator ==(LogContext x, LogContext y) => x.Equals(y);

        public static bool operator !=(LogContext x, LogContext y) => !x.Equals(y);

        public string CustomValue{ get; }

        public static implicit operator LogContext(string s) => new(s);

        public static implicit operator string(LogContext context) => context.CustomValue switch{
            nameof(All) => ".*",
            nameof(None) => Guid.NewGuid().ToString(),
            _ => context.CustomValue
        };

        
    }

}