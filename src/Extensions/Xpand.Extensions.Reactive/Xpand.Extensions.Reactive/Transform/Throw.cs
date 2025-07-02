using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.Reactive.Combine;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<T> ThrowOnNext<T>(this IObservable<T> source, [CallerMemberName] string caller = "")
            => source.SelectMany(arg => new InvalidOperationException($"{arg} - {caller}").Throw<T>());

        public static IObservable<Unit> ThrowUnit(this Exception exception,int skipFrame=0)
            => exception.Throw<Unit>(skipFrame+1);
        public static IObservable<object> ThrowObject(this Exception exception,int skipFrame=0)
            => exception.Throw<object>(skipFrame+1);
        
        public static IObservable<T> Throw<T>(this Exception exception,int skipFrame=0)
            => exception.CaptureStack(skipFrame+1).Observe().SelectMany(Observable.Throw<T>);
        
        public static IObservable<T> ThrowIfEmpty<T>(this IObservable<T> source,[CallerMemberName]string caller="")
            => source.SwitchIfEmpty(Observable.Defer(() => new SequenceIsEmptyException($"source is empty {caller}").Throw<T>()));
        
        public static IObservable<T> ThrowIfEmpty<T>(this IObservable<T> source,Exception exception) {
            exception = exception.CaptureStack(1);
            return source.SwitchIfEmpty(Observable.Defer(() => exception.Throw<T>()));
        }
    }
    
    public class SequenceIsEmptyException(string message) : Exception(message);

}