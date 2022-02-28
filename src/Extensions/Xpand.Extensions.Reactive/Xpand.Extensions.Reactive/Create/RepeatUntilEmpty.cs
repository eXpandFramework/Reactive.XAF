using System;
using System.Reactive;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Create {
    public static partial class Create {
        public static IObservable<T> RepeatUntilEmpty<T>(this IObservable<T> source) 
            => source.Materialize()
                .Repeat()
                .StartWith((Notification<T>)null)
                .Buffer(2, 1)
                .Select(it => {
                    Console.WriteLine($"Buffer content: {String.Join(",", it)}");
                    if (it[1].Kind != NotificationKind.OnCompleted) {
                        return it[1];
                    }

                    // it[1] is OnCompleted, check the previous one
                    if (it[0] != null && it[0].Kind != NotificationKind.OnCompleted) {
                        // not a consecutive OnCompleted, so we ignore this OnCompleted with a NULL marker
                        return null;
                    }

                    // okay, we have two consecutive OnCompleted, stop this observable.
                    return it[1];
                })
                .Where(it => it != null) // remove the NULL marker
                .Dematerialize();
    }
}