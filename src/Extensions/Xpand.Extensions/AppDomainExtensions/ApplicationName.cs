using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Fasterflect;

namespace Xpand.Extensions.AppDomainExtensions{
    public static partial class AppDomainExtensions {
        private static readonly ConcurrentDictionary<string,string> StringCache = new();
        [SuppressMessage("ReSharper", "HeapView.CanAvoidClosure")]
        public static string GetOrAdd(this string key) => StringCache.GetOrAdd(key, _ => key);

        public static string ApplicationName(this AppDomain appDomain){
            if (appDomain.UseNetFramework()){
                var setupInformation = AppDomain.CurrentDomain.GetPropertyValue("SetupInformation");
                return (string) (setupInformation.GetPropertyValue("ApplicationName")) ;
            }

            return Assembly.GetEntryAssembly()?.GetName().Name??"ApplicationName";
        }
    }
}