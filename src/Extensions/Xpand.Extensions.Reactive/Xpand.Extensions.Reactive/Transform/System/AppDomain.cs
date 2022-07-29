using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.StreamExtensions;

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

        public static IObservable<string> WhenFileCreated(this AppDomain appDomain,string path,string pattern)
            => Observable.Using(() => new FileSystemWatcher(path, pattern){ EnableRaisingEvents = true }, watcher => watcher
                .WhenEvent<FileSystemEventArgs>(nameof(FileSystemWatcher.Created))
                .SelectMany(args => AppDomain.CurrentDomain.OpenFile(args.FullPath)));


        public static IObservable<string> WhenFileReadAsString(this AppDomain appDomain,string fileName,FileMode fileMode=FileMode.Open,FileAccess fileAccess=FileAccess.Read,FileShare fileShare=FileShare.Read) 
            => File.Open(fileName, fileMode, fileAccess, fileShare).ReadToEndAsStringAsync().ToObservable().RetryWithBackoff();

        public static IObservable<string> WhenDirectory(this AppDomain appDomain,string directory,bool create=true) {
            if (!Directory.Exists(directory)){
                if (create) {
                    Directory.CreateDirectory(directory!);
                    return directory.ReturnObservable();
                }

                return Observable.Empty<string>();
            }
            return directory.ReturnObservable();
        }

        public static IObservable<string> OpenFile(this AppDomain appDomain,string fileName,FileMode fileMode=FileMode.Open,FileAccess fileAccess=FileAccess.Read,FileShare fileShare=FileShare.Read) 
            => Observable.Defer(() => Observable.Using(() => File.Open(fileName, fileMode, fileAccess, fileShare),
                    _ => {
                        _.Dispose();
                        return fileName.ReturnObservable();
                    }))
                .RetryWithBackoff(strategy:_ => TimeSpan.FromMilliseconds(200));


        public static IObservable<AppDomain> ExecuteOnce(this AppDomain appDomain) 
            => AppdomainOneEmission.AsObservable();
    }
}
