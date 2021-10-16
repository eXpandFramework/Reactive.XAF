using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility{
    public static partial class Utility {
        public static IObservable<T> ReplayConnect<T>(this IObservable<T> source, IScheduler scheduler,
            int bufferSize = 0)
            => source.SubscribeReplay(scheduler, bufferSize);
        public static IObservable<T> SubscribeReplay<T>(this IObservable<T> source, IScheduler scheduler,int bufferSize = 0) 
            => source.SubscribeOn(scheduler ?? Scheduler.Default).SubscribeReplay(bufferSize);

        public static IObservable<T> SubscribeReplay<T>(this IObservable<T> source, int bufferSize = 0){
            var replay = bufferSize > 0 ? source.Replay(bufferSize) : source.Replay();
            replay.Connect();
            return replay;
        }
        public static IObservable<T> ReplayConnect<T>(this IObservable<T> source, int bufferSize = 0) 
            => source.SubscribeReplay(bufferSize);
    }
}