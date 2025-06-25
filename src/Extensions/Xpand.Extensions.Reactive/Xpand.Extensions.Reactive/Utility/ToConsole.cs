using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Tracing;
using static System.Console;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> ToConsole<T>(
            this IObservable<T> source, 
            RXAction action, 
            Func<TimeSpan, T, object> nextSelector, 
            [CallerMemberName] string caller = "") 
        {
            var stopwatch = new Stopwatch();
    
            return source
                .DoOnSubscribe(() => {
                    if (action.HasFlag(RXAction.Subscribe)) {
                        stopwatch.Start();
                        var value = $"{caller} - Subscribe";
                        WriteLine(value); 
                        Debug.WriteLine(value);
                    }
                })
                .Finally(() => {
                    if (!action.HasFlag(RXAction.Dispose)) return;
                    WriteLine($"{caller} - Dispose");
                    stopwatch.Stop();
                })
                .DoWhen(_ => action.HasFlag(RXAction.OnNext) && !stopwatch.IsRunning,_ => stopwatch.Start())
                .Select(obj => {
                    if (!action.HasFlag(RXAction.OnNext)) return obj;
                    stopwatch.Stop();
                    WriteLine($"{caller} - OnNext - {nextSelector?.Invoke(stopwatch.Elapsed, obj)??stopwatch.Elapsed}");
                    stopwatch.Restart(); 
                    return obj;
                })
                .DoOnError(exception => {
                    if (!action.HasFlag(RXAction.OnError)) return;
                    WriteLine($"{caller} - OnError - {exception.Message}");
                })
                .DoOnComplete(() => {
                    if (!action.HasFlag(RXAction.OnCompleted)) return;
                    WriteLine($"{caller} - OnCompleted");
                });
        }        
        public static IObservable<T> ToConsole<T>(this IObservable<T> source, RXAction action, [CallerMemberName] string caller = "") 
            => source.ToConsole(action, null, caller);

        public static IObservable<T> ToConsole<T>(this IObservable<T> source, Func<T,int, object> msgSelector ,[CallerMemberName]string caller="")
            => source.Do((obj, i) => obj.Write(arg =>msgSelector?.Invoke(arg, i) ,caller));
        
        public static IObservable<T> ToConsole<T>(this IObservable<T> source, Func<T, object> msgSelector = null,[CallerMemberName]string caller="")
            => source.Do(obj => obj.Write(msgSelector, caller));
        public static IObservable<T> ToConsole<T>(this IObservable<T> source,Action<TimeSpan> onMeasured, Func<T, object> msgSelector = null,[CallerMemberName]string caller="")
            => source.ToConsole(msgSelector,caller).MeasureCompletionTime(onMeasured);
        
        public static IObservable<T> MeasureCompletionTime<T>(this IObservable<T> source, Action<TimeSpan> onMeasured)
            => Observable.Defer(() => {
                var sw = Stopwatch.StartNew();
                return source.Do(onNext: _ => { }, onCompleted: () => {
                        sw.Stop();
                        onMeasured(sw.Elapsed);
                    });
            });

        private static void Write<T>(this T obj,Func<T, object> msgSelector, string caller){
            var value = msgSelector != null ? $"{msgSelector(obj)}" : $"{obj}";
            if (value == string.Empty) return;
            WriteLine($"{caller} - {value}");
        }
    }
}