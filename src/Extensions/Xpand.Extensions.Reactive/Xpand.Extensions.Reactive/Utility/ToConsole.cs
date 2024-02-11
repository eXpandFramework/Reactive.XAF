using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using akarnokd.reactive_extensions;
using Xpand.Extensions.Tracing;
using static System.Console;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> ToConsole<T>(this IObservable<T> source, RXAction action, [CallerMemberName] string caller = "") {
            void HandleAction(RXAction flag, Action act) {
                if (action.HasFlag(flag)) act();
            }
            HandleAction(RXAction.Subscribe, () => source = source.DoOnSubscribe(() => WriteLine($"{caller} - Subscribe")));
            HandleAction(RXAction.Dispose, () => source = source.Finally(() => WriteLine($"{caller} - Dispose")));
            HandleAction(RXAction.OnNext, () => source = source.Do(obj => WriteLine($"{caller} - OnNext - {obj}")));
            HandleAction(RXAction.OnError, () => source = source.DoOnError(exception => WriteLine($"{caller} - OnError - {exception.Message}")));
            HandleAction(RXAction.OnCompleted, () => source = source.DoOnComplete(() => WriteLine($"{caller} - OnCompleted")));

            return source;
        }
        public static IObservable<T> ToConsole<T>(this IObservable<T> source, Func<T, object> msgSelector = null,[CallerMemberName]string caller="")
            => source.Do(obj => {
                var value = msgSelector != null ? $"{msgSelector(obj)}" : $"{obj}";
                if (value == string.Empty) return;
                WriteLine($"{caller} - {value}");
            });
    }
}