using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Tracing;
using static System.Console;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> ToConsole<T>(this IObservable<T> source, RXAction action, Func<TimeSpan, T, object> nextSelector, [CallerMemberName] string caller = "") {
            var stopwatch = new Stopwatch();
            return source.DoOnSubscribe(() => {
                    if (!action.HasFlag(RXAction.Subscribe)) return;
                    stopwatch.Start();
                    var value = $"{caller} - Subscribe".Trim(" - ".ToCharArray());
                    WriteLine(value); 
                    Debug.WriteLine(value);
                })
                .Finally(() => {
                    if (!action.HasFlag(RXAction.Dispose)) return;
                    WriteLine($"{caller} - Dispose".Trim(" - ".ToCharArray()));
                    stopwatch.Stop();
                })
                .DoWhen(_ => action.HasFlag(RXAction.OnNext) && !stopwatch.IsRunning,_ => stopwatch.Start())
                .Select(obj => {
                    if (!action.HasFlag(RXAction.OnNext)) return obj;
                    stopwatch.Stop();
                    WriteLine($"{caller} - OnNext - {nextSelector?.Invoke(stopwatch.Elapsed, obj)??stopwatch.Elapsed}".Trim(" - ".ToCharArray()));
                    stopwatch.Restart(); 
                    return obj;
                })
                .DoOnError(exception => {
                    if (!action.HasFlag(RXAction.OnError)) return;
                    WriteLine($"{caller} - OnError - {exception.Message}".Trim(" - ".ToCharArray()));
                })
                .DoOnComplete(() => {
                    if (!action.HasFlag(RXAction.OnCompleted)) return;
                    WriteLine($"{caller} - OnCompleted".Trim(" - ".ToCharArray()));
                });
        }        
        public static IObservable<T> ToConsole<T>(this IObservable<T> source, RXAction action, [CallerMemberName] string caller = "") 
            => source.ToConsole(action, null, caller);

        public static IObservable<T> ToConsole<T>(this IObservable<T> source, Func<T,int, object> msgSelector ,[CallerMemberName]string caller="")
            => source.Do((obj, i) => obj.Write(arg =>msgSelector?.Invoke(arg, i) ,caller));
        
        public static IObservable<T> ToConsole<T>(this IObservable<T> source, Func<T, object> msgSelector = null,[CallerMemberName]string caller="",bool measureTime=false) {
            var stopwatch = new Stopwatch();
            if (measureTime) stopwatch.Start();
            return source.Do(obj => {
                    stopwatch.Stop();
                    obj.Write(msgSelector, caller, stopwatch.Elapsed.TotalSeconds.NearlyEquals(0)?null:stopwatch.Elapsed);
                });
        }

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

        private static void Write<T>(this T obj,Func<T, object> msgSelector, string caller, TimeSpan? timeSpan=null){
            var value = msgSelector != null ? $"{msgSelector(obj)}" : $"{(obj is Unit ? null : obj)}";
            var time =timeSpan.HasValue?$"{timeSpan.Value.TotalSeconds.RoundNumber()}sec": null;
            WriteLine(new[]{new[]{caller,value}.WhereNotNullOrEmpty().JoinDashSpaces(), time}.WhereNotDefault().JoinColonSpace());
        }
    }
}