using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.AppDomainExtensions{
    public static partial class AppDomainExtensions {
        private static readonly ConcurrentDictionary<string,string> StringCache = new();
        [SuppressMessage("ReSharper", "HeapView.CanAvoidClosure")]
        public static string GetOrAdd(this string key) => StringCache.GetOrAdd(key, _ => key);
        public static void KillAll(this AppDomain appDomain,string processName) 
            => Process.GetProcessesByName(processName).WhereDefault(process => process.HasExited)
                .Do(process => {
                    try {
                        process.Kill();
                        process.WaitForExit();
                    }
                    catch {
                        // ignored
                    }
                }).Enumerate();

        public static string ApplicationName(this AppDomain appDomain){
            if (appDomain.UseNetFramework()){
                var setupInformation = AppDomain.CurrentDomain.GetPropertyValue("SetupInformation");
                return (string) (setupInformation.GetPropertyValue("ApplicationName")) ;
            }

            return Assembly.GetEntryAssembly()?.GetName().Name??"ApplicationName";
        }
    }
}