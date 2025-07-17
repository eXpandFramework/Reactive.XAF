using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using Xpand.Extensions.Reactive.Combine;

namespace Xpand.Extensions.Reactive.ErrorHandling {
    public static partial class ErrorHandling {
        const string Key = "Origin";
        static readonly string[] InternalNs = ["System.Reactive."];
        static readonly ConcurrentDictionary<string, Lazy<StackTrace>> Cache = new();
        
        public static IObservable<T> WithOrigin<T>(this IObservable<T> source) {
            var origin = new Lazy<StackTrace>(() => new StackTrace(true), LazyThreadSafetyMode.PublicationOnly);
            return Observable.Create<T>(o =>
                source.Subscribe(o.OnNext,
                    e => { if (!e.Data.Contains(Key)) e.Data[Key] = origin.Value; o.OnError(e); },
                    o.OnCompleted));
        }

        static StackTrace Capture() {
            var full=new StackTrace(true);
            var frames=full.GetFrames();
            var skip=Array.FindIndex(frames,f=>{
                var n=f.GetMethod()?.DeclaringType?.FullName;
                return n!=null&&InternalNs.All(p=>!n.StartsWith(p));
            });
            return Cache.GetOrAdd((skip<0?0:skip).ToString(),
                    _=>new Lazy<StackTrace>(()=>new StackTrace(skip<0?0:skip,true)))
                .Value;
        }

        public static Exception Tag(this Exception ex) {
            if (ex.Data.Contains(Key)) return ex;
            ex.Data[Key] = Capture();
            return ex;
        }

        public static IObservable<T> Throw<T>(this Exception ex) 
            => Observable.Defer(() => {
                ExceptionDispatchInfo.Capture(ex.Tag()).Throw(); 
                return Observable.Empty<T>();                    
            });
        
        public static IObservable<T> ThrowOnNext<T>(this IObservable<T> source, [CallerMemberName] string caller = "")
            => source.SelectMany(arg => new InvalidOperationException($"{arg} - {caller}").Throw<T>());

        public static IObservable<Unit> ThrowUnit(this Exception exception)
            => exception.Throw<Unit>();
        public static IObservable<object> ThrowObject(this Exception exception)
            => exception.Throw<object>();
        
        public static IObservable<T> ThrowIfEmpty<T>(this IObservable<T> source,[CallerMemberName]string caller="")
            => source.SwitchIfEmpty(Observable.Defer(() => new SequenceIsEmptyException($"source is empty {caller}").Throw<T>()));
        
        public static IObservable<T> ThrowIfEmpty<T>(this IObservable<T> source,Exception exception) 
            => source.SwitchIfEmpty(exception.Throw<T>());
    }
    public class SequenceIsEmptyException(string message) : Exception(message);
}