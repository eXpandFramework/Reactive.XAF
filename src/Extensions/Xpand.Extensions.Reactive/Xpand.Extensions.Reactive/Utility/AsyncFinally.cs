using System;
using System.Reactive;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility{
    public static partial class Utility{
        public static IObservable<T> AsyncFinally<T>(this IObservable<T> source, Func<System.Threading.Tasks.Task> action){
            return source
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
                    .Dematerialize()
                ;
        }
    }
}