using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Xpand.Extensions.Reactive.Conditional;

namespace Xpand.Extensions.Reactive.Utility{
    public static partial class Utility {
        public static IObservable<T> ReplayFirstTake<T>(this IObservable<T> source,ConnectionMode mode=ConnectionMode.AutoConnect){
            var takeAndReplay = source.TakeAndReplay(1);
            return mode==ConnectionMode.AutoConnect?takeAndReplay.AutoConnect():takeAndReplay.RefCount();
        }

        public static IObservable<T> AutoConnectAndReplayTake<T>(this IObservable<T> source) 
            => source.TakeAndReplay(1).AutoConnect();
        
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
    
    public enum ConnectionMode{
        AutoConnect,
        RefCount
    }

}