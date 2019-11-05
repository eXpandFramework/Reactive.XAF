using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Transform.System.Net{
    public static class SystemNetTransform{
        
        public static IObservable<IPEndPoint> Listening(this IEnumerable<IPEndPoint> source,bool repeatWhenOffine=true,TimeSpan? timeSpan=null){
//            return source.ToArray().AsObservable()
//                .SelectMany(points => {
//                    return Observable.Start(() => {
//                        
//                        var notInUsed = points.ToObservable(Scheduler.Default).Where(point => point.Listening());
//                        var inUsed = points.ToObservable(Scheduler.Default).Where(point => point.Listening())
//                            .Select(point => notInUsed.Where(nu => point));
//                        return inUsed
//                            .RepeatWhen(obs => obs.Where())
//                    });
//                });
            timeSpan = timeSpan ?? TimeSpan.FromMilliseconds(500);
            return source.ToObservable()
                .SelectMany(endPoint => {
                    var inUsed = Observable.While(() => !endPoint.Listening(), Observable.Empty<IPEndPoint>().Delay(timeSpan.Value))
                        .Concat(endPoint.AsObservable());
                    var notInUse = Observable.While(endPoint.Listening, Observable.Empty<IPEndPoint>().Delay(timeSpan.Value))
                        .Concat(endPoint.AsObservable());
                    return repeatWhenOffine ? inUsed.RepeatWhen(_ => _.SelectMany(o => notInUse)) : inUsed;
                });
        }

        public static bool Listening(this IPEndPoint endPoint){
            return IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Contains(endPoint);
        }
    }
}