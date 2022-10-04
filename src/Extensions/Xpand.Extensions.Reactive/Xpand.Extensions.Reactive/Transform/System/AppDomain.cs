using System;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Xpand.Extensions.Reactive.Transform.System.Net;

namespace Xpand.Extensions.Reactive.Transform.System {
    public static class AppDomainExtensions {
        static readonly IConnectableObservable<AppDomain> AppdomainOneEmission;
        static AppDomainExtensions() {
            AppdomainOneEmission = AppDomain.CurrentDomain.ReturnObservable().BufferUntilSubscribed();
            AppdomainOneEmission.Connect();
        }
        public static IObservable<Assembly> WhenAssemblyLoad(this AppDomain appDomain) 
            => Observable.FromEventPattern<AssemblyLoadEventHandler, AssemblyLoadEventArgs>(
                    h => appDomain.AssemblyLoad += h, h => appDomain.AssemblyLoad -= h,ImmediateScheduler.Instance)
                .Select(pattern => pattern.EventArgs.LoadedAssembly);

        public static IObservable<ResolveEventArgs> WhenAssemblyResolve(this AppDomain appDomain)
	        => appDomain.WhenEvent<ResolveEventArgs>(nameof(AppDomain.AssemblyResolve));

        public static IObservable<AppDomain> ExecuteOnce(this AppDomain appDomain) 
            => AppdomainOneEmission.AsObservable();
        
        public static HttpClient HttpClient(this AppDomain domain) => NetworkExtensions.HttpClient;
    }
}
