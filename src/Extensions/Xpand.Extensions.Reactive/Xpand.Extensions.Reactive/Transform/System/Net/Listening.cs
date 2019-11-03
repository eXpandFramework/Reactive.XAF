using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform.System.Net{
    public static class SystemNetTransform{
        public static IObservable<IPEndPoint> Listening(this IPEndPoint[] source,bool repeatWhenOffine=true){
            return source.ToObservable(Scheduler.Default)
                .SelectMany(endPoint => {
                    var inUsed = Observable.While(() => !endPoint.Listening(), Observable.Empty<IPEndPoint>().ObserveOn(Scheduler.Default))
                        .Concat(endPoint.AsObservable());
                    var notInUse = Observable.While(endPoint.Listening, Observable.Empty<IPEndPoint>().ObserveOn(Scheduler.Default));
                    return repeatWhenOffine ? inUsed.RepeatWhen(_ => _
                        .SelectMany(o => notInUse.Concat(endPoint.AsObservable()))) : inUsed;
                });
        }

        public static bool Listening(this IPEndPoint endPoint){
            return IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Contains(endPoint);
        }
    }
}