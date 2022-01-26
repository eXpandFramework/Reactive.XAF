using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace Xpand.Extensions.Reactive.Utility{
    public static partial class Utility {
        public static IObservable<T> AsyncFinally<T>(this IObservable<T> source, Func<IObservable<object>> action)
            => source.AsyncFinally(async () => await action().ToTask());

	    public static IObservable<T> AsyncFinally<T>(this IObservable<T> source, Func<System.Threading.Tasks.Task> action) 
            => source
                .Materialize()
                .SelectMany(async n => {
                    switch (n.Kind){
                        case NotificationKind.OnCompleted:
                        case NotificationKind.OnError:
                            await action();
                            return n;
                        case NotificationKind.OnNext:
                            return n;
                        default:
                            throw new NotImplementedException();
                    }
                })
                .Dematerialize();
    }
}