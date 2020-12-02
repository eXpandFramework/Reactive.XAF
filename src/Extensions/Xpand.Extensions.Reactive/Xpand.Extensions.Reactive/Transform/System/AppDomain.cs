using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;

namespace Xpand.Extensions.Reactive.Transform.System {
    public static class AppDomainExtensions {
        static readonly IConnectableObservable<AppDomain> AppdomainOneEmmision;
        static AppDomainExtensions() {
            AppdomainOneEmmision = AppDomain.CurrentDomain.ReturnObservable().BufferUntilSubscribed();
            AppdomainOneEmmision.Connect();
        }
        public static IObservable<Assembly> WhenAssemblyLoad(this AppDomain appDomain) 
            => Observable.FromEventPattern<AssemblyLoadEventHandler, AssemblyLoadEventArgs>(
                    h => appDomain.AssemblyLoad += h, h => appDomain.AssemblyLoad -= h,ImmediateScheduler.Instance)
                .Select(pattern => pattern.EventArgs.LoadedAssembly);

        public static IObservable<ResolveEventArgs> WhenAssemblyResolve(this AppDomain appDomain) 
            => Observable.FromEventPattern<ResolveEventHandler, ResolveEventArgs>(
                    h => appDomain.AssemblyResolve += h, h => appDomain.AssemblyResolve -= h,ImmediateScheduler.Instance)
                .Select(pattern => pattern.EventArgs);
        
    public static IObservable<AppDomain> ExecuteOnce(this AppDomain appDomain) 
            => AppdomainOneEmmision.AsObservable();
    }
}
