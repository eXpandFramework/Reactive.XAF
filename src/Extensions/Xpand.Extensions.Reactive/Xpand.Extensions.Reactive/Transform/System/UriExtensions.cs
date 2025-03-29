using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using Xpand.Extensions.ProcessExtensions;

namespace Xpand.Extensions.Reactive.Transform.System {
    public static class UriExtensions {
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