using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> ToConsole<T>(this IObservable<T> source, Func<T, object> msgSelector = null,[CallerMemberName]string caller="")
            => source.Do(obj => {
                var value = msgSelector != null ? $"{msgSelector(obj)}" : $"{obj}";
                if (value == string.Empty) return;
                Console.WriteLine($"{caller} - {value}");
            });
    }
}