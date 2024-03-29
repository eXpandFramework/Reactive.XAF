﻿using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.Combine;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<T> ThrowOnNext<T>(this IObservable<T> source, [CallerMemberName] string caller = "")
            => source.SelectMany(arg => new InvalidOperationException($"{arg} - {caller}").Throw<T>());
        
        public static IObservable<T> Throw<T>(this Exception exception)
            => Observable.Throw<T>(exception);
        
        public static IObservable<T> ThrowIfEmpty<T>(this IObservable<T> source,[CallerMemberName]string caller="")
            => source.SwitchIfEmpty(Observable.Defer(() => new SequenceIsEmptyException($"source is empty {caller}").Throw<T>()));
        
        public static IObservable<T> ThrowIfEmpty<T>(this IObservable<T> source,Exception exception)
            => source.SwitchIfEmpty(Observable.Defer(exception.Throw<T>));
    }
    
    public class SequenceIsEmptyException:Exception {
        public SequenceIsEmptyException(string message) : base(message){
        }
    }

}