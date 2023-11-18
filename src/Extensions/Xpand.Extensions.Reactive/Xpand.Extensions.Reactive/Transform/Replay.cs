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
        
        public enum ConnectionMode{
            AutoConnect,
            RefCount
        }

    }
}