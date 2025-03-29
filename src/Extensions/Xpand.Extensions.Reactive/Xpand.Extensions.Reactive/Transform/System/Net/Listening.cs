using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.Transform.System.Net{
    public static class SystemNetTransform{
        private static readonly IObservable<IPAddress> LocalHost;

        static SystemNetTransform() => LocalHost = Dns.GetHostAddressesAsync("localhost").ToObservable().SelectMany().LastAsync().ReplayConnect(1);

        public static IObservable<IPEndPoint> Listening(this IObservable<IPEndPoint> source,bool repeatWhenOffline=true,TimeSpan? timeSpan=null) 
            => LocalHost.CombineLatest(source, (localHost, endPoint) => endPoint.Address.Equals(localHost) 
                    ? endPoint.LocalHostListening(repeatWhenOffline,timeSpan ??= TimeSpan.FromMilliseconds(500)) : endPoint.Ping()).Merge();

        private static IObservable<IPEndPoint> LocalHostListening(this IPEndPoint endPoint,bool repeatWhenOffline, TimeSpan timeSpan) {
            var inUsed = Observable.While(() => !endPoint.Listening(), Observable.Empty<IPEndPoint>().Delay(timeSpan))
                .Concat(endPoint.Observe());
            var notInUse = Observable.While(endPoint.Listening, Observable.Empty<IPEndPoint>().Delay(timeSpan))
                .Concat(endPoint.Observe());
            return repeatWhenOffline ? inUsed.RepeatWhen(obs => obs.SelectMany(_ => notInUse)) : inUsed;
        }

        public static IObservable<(IPAddress address,int port)> Ping(this IPAddress address, int port) 
            => Observable.Using(() => new TcpClient(), client => {
                var result = client.BeginConnect(address, port, null, null);
                result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(1500));
                client.EndConnect(result);
                return (address, port).Observe();
            }).CompleteOnError();

        public static IObservable<IPEndPoint> Ping(this IPEndPoint endPoint) 
            => endPoint.Address.Ping(endPoint.Port).To(endPoint);

        public static bool Listening(this IPEndPoint endPoint) => IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Contains(endPoint);
        public static bool IsFree(this IPEndPoint endPoint) => !endPoint.Listening();
        

    }
}