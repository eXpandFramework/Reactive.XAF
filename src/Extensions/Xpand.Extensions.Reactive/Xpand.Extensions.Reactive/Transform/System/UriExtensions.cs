using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using Xpand.Extensions.Network;
using Xpand.Extensions.ProcessExtensions;
using Xpand.Extensions.Reactive.Transform.System.Net;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.Transform.System {
    public static class UriExtensions {
        public static IObservable<(IPEndPoint Value, TimeSpan Duration)> WhenPing(this Uri uri){
            var addresses = uri.IPAddresses().First();
            return new IPEndPoint(addresses, uri.Port).Ping().WithEmissionTime();
        }
        public static IObservable<Process> Start(this Uri uri,string browser=null) 
            => new ProcessStartInfo{
                FileName = browser??"chrome",
                Arguments = $"--user-data-dir={CreateTempProfilePath(browser)} --no-first-run --no-default-browser-check {uri}",
                UseShellExecute = true
            }.Start().Observe().Delay(TimeSpan.FromSeconds(2));

        private static string CreateTempProfilePath(string name){
            var path = $"{Path.GetTempPath()}\\{name}";
            if (!Directory.Exists(path)){
                Directory.CreateDirectory($"{Path.GetTempPath()}\\{name}");
            }

            path = $"{Path.GetTempPath()}\\{name}\\{Guid.NewGuid():N}";
            Directory.CreateDirectory(path);
            return path;
        }
        
        

    }
}