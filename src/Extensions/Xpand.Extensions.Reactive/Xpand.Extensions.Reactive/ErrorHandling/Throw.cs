using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;

namespace Xpand.Extensions.Reactive.ErrorHandling {
    public static partial class ErrorHandling {
        const string Key = "Origin";

        public static Exception TagOrigin(this Exception ex)
            => ex.AccessData(data => {
                if (data.Contains(Key)) return ex;
                data[Key] = ChainFaultContextService.GetOrAddCachedStackTraceString();
                return ex;
            });
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "ConvertToLambdaExpression")]
        public static IObservable<T> Throw<T>(this Exception ex) 
            => Observable.Defer(() => Observable.Throw<T>(ex.TagOrigin()));
        
        public static IObservable<T> ThrowOnNext<T>(this IObservable<T> source, [CallerMemberName] string caller = "")
            => source.SelectMany(arg => new InvalidOperationException($"{arg} - {caller}").Throw<T>());

        public static IObservable<Unit> ThrowUnit(this Exception exception)
            => exception.Throw<Unit>();
        
        public static IObservable<object> ThrowObject(this Exception exception)
            => exception.Throw<object>();
        
        public static IObservable<T> ThrowIfEmpty<T>(this IObservable<T> source, [CallerMemberName] string caller = "")
            => source.SwitchIfEmpty(Observable.Defer(() => new SequenceIsEmptyException($"source is empty {caller}").Throw<T>()));
        
        public static IObservable<T> ThrowIfEmpty<T>(this IObservable<T> source, Exception exception) 
            => source.SwitchIfEmpty(exception.Throw<T>());
    }

    public class SequenceIsEmptyException(string message) : Exception(message);
}