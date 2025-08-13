using System;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Conditional;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<T> AutoConnectAndReplayTake<T>(this IObservable<T> source) 
            => source.TakeAndReplay(1).AutoConnect();
        public static IObservable<T> ReplayFirstTake<T>(this IObservable<T> source,
            ConnectionMode mode = ConnectionMode.AutoConnect) {
            var takeAndReplay = source.TakeAndReplay(1);
            return mode == ConnectionMode.AutoConnect ? takeAndReplay.AutoConnect() : takeAndReplay.RefCount();
        }

        public static IObservable<T> AutoReplayFirst<T>(this IObservable<T> source)
            => source.AutoReplay(1);
        public static IObservable<T> AutoReplay<T>(this IObservable<T> source,int count) 
            => source.Replay(count).AutoConnect(0);

        public enum ConnectionMode{
            AutoConnect,
            RefCount
        }

    }
}