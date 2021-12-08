using System;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility{
    public static partial class Utility{
        public static IObservable<T> PublishConnect<T>(this IObservable<T> source) 
            => source.SubscribePublish();

        public static IObservable<T> SubscribePublish<T>(this IObservable<T> source){
            var publish = source.Publish();
            publish.Connect();
            return publish;
        }
    }
}