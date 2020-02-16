using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility{
    public static partial class Utility{
        public static IObservable<T> SubscribeReplay<T>(this IObservable<T> source, IScheduler scheduler,int bufferSize = 0){
            scheduler ??= Scheduler.Default;
            var subscribeOn = source.SubscribeOn(scheduler);
            return subscribeOn.SubscribeReplay(bufferSize);
        }

        public static IObservable<T> SubscribeReplay<T>(this IObservable<T> source, int bufferSize = 0){
            var replay = bufferSize > 0 ? source.Replay(bufferSize) : source.Replay();
            replay.Connect();
            return replay;
        }
    }
}