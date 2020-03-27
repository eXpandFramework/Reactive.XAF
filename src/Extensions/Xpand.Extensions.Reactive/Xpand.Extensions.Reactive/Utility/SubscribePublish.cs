using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility{
    public static partial class Utility{
        public static IObservable<T> SubscribePublish<T>(IObservable<T> source){
            var publish = source.Publish();
            publish.Connect();
            return publish;
        }
    }
}