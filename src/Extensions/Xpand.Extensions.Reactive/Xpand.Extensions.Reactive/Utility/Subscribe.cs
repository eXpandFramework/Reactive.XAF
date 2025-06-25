using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Utility{
    public static partial class Utility {
        public static IObservable<T> Unsubscribed<T>(this IObservable<T> source, Action unsubscribed) 
            => Observable.Create<T>(o => new CompositeDisposable(source.Subscribe(o), Disposable.Create(unsubscribed)));
        
        public static IObservable<T> ReplayConnect<T>(this IObservable<T> source, IScheduler scheduler,
            int bufferSize = 0)
            => source.SubscribeReplay(scheduler, bufferSize);
        public static IObservable<T> SubscribeReplay<T>(this IObservable<T> source, IScheduler scheduler,int bufferSize = 0) 
            => source.SubscribeOn(scheduler ?? System.Reactive.Concurrency.Scheduler.Default).SubscribeReplay(bufferSize);

        public static IObservable<T> SubscribeReplay<T>(this IObservable<T> source, int bufferSize = 0){
            var replay = bufferSize > 0 ? source.Replay(bufferSize) : source.Replay();
            replay.Connect();
            return replay;
        }
        public static IObservable<T> ReplayConnect<T>(this IObservable<T> source, int bufferSize = 0) 
            => source.SubscribeReplay(bufferSize);
        
        public static IObservable<T> SubscribeOnDefault<T>(this IObservable<T> source)
            => source.SubscribeOn(DefaultScheduler.Instance);
        
        public static IObservable<T> PublishConnect<T>(this IObservable<T> source) 
            => source.SubscribePublish();
        

        public static IObservable<T> SubscribePublish<T>(this IObservable<T> source){
            var publish = source.Publish();
            publish.Connect();
            return publish;
        }
        
        public static IObservable<T2> SequentialResubscribe<T, T2>(this IObservable<T> source, Func<T, IObservable<T2>> selector) 
            => source.BufferUntilCompleted()
                .SelectMany(items => {
                    var count = 0;
                    return Observable.Defer(() => source.Skip(count++).Take(1).SelectMany(selector)).Repeat()
                        .Take(items.Length);
                });
    }
}