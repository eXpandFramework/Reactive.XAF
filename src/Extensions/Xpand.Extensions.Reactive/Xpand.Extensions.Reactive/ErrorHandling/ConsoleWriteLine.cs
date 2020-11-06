using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.ErrorHandling {
    public static partial class ErrorHandling {
        public static IObservable<T> ConsoleWriteLine<T>(this IObservable<T> source,Func<T,object> modify) 
            => source.Do(obj => Console.WriteLine(modify(obj)));

        public static IObservable<T> ConsoleWriteLine<T>(this IObservable<T> source) 
            => source.Do(obj => Console.WriteLine(obj));
    }
}