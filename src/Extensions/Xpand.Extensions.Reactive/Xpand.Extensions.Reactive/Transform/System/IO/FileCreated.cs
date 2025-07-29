using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using Xpand.Extensions.FileExtensions;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.StreamExtensions;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Reactive.Transform.System.IO {
    public static class DirectoryInfoExtensions {
        public static IObservable<FileInfo> WhenFileCreated(this DirectoryInfo directoryInfo,string pattern=null) {
            var infoClone = new DirectoryInfo(directoryInfo.FullName);
            if (!infoClone.Exists) {
                directoryInfo = directoryInfo.ParentExists();
                return Observable.Using(() => new FileSystemWatcher(directoryInfo.FullName) { EnableRaisingEvents = true,IncludeSubdirectories = true}, 
                    watcher => watcher.WhenEvent<FileSystemEventArgs>(nameof(FileSystemWatcher.Created)).TakeUntil(watcher.WhenDisposed())
                        .Where(e => new[]{WatcherChangeTypes.Created,WatcherChangeTypes.Renamed}.Contains(e.ChangeType))
                        .Publish(source =>source.Where(e => File.Exists(e.FullPath))
                            .Where(e =>Path.GetDirectoryName(e.FullPath)==infoClone.FullName&& (pattern==null||Regex.IsMatch(Path.GetFileName(e.FullPath),
                                pattern.WildCardToRegular())) )) )
                    .Select(e => new FileInfo(e.FullPath));
            }
            return Observable.Using(() => new FileSystemWatcher(directoryInfo.FullName, pattern??"*"){ EnableRaisingEvents = true }, watcher => watcher
                .WhenEvent<FileSystemEventArgs>(nameof(FileSystemWatcher.Created)).TakeUntil(watcher.WhenDisposed())
                .Select(args => new FileInfo(args.FullPath)));
        }

        public static IObservable<FileInfo> WhenCreated(this FileInfo fileInfo) 
            => fileInfo.Directory.WhenFileCreated(fileInfo.Name)
                .Merge(fileInfo.Exists.Observe().WhenNotDefault().To(fileInfo));
        
        public static IObservable<FileInfo> WhenChanged(this FileInfo fileInfo) 
            => fileInfo.When(nameof(FileSystemWatcher.Changed));

        private static IObservable<FileInfo> When(this FileInfo fileInfo,string eventName) 
            => Observable.Using(() => new FileSystemWatcher(fileInfo.DirectoryName!,fileInfo.Name){ EnableRaisingEvents = true }, watcher => watcher
                .WhenEvent<FileSystemEventArgs>(eventName).TakeUntil(watcher.WhenDisposed())
                .Select(args => new FileInfo(args.FullPath)));

        public static IObservable<FileInfo> WhenDeleted(this FileInfo fileInfo) 
            => fileInfo.When(nameof(FileSystemWatcher.Deleted));
        
        public static IObservable<FileInfo> WhenRenamed(this FileInfo fileInfo) 
            => fileInfo.When(nameof(FileSystemWatcher.Renamed));

        public static IObservable<string> WhenFileReadAsString(this FileInfo fileInfo,
            FileMode fileMode = FileMode.OpenOrCreate, FileAccess fileAccess = FileAccess.Read, FileShare fileShare = FileShare.Read,int retry=5) 
            => fileInfo.WhenFileRead(stream => stream.ReadToEndAsStringAsync().ToObservable(),fileMode,fileAccess,fileShare,retry);
        
        public static IObservable<byte[]> WhenFileReadAsBytes(this FileInfo fileInfo,
            FileMode fileMode = FileMode.OpenOrCreate, FileAccess fileAccess = FileAccess.Read, FileShare fileShare = FileShare.Read,int retry=5) 
            => fileInfo.WhenFileRead(stream => stream.Bytes().Observe(),fileMode,fileAccess,fileShare,retry);

        public static IObservable<T> WhenFileRead<T>(this FileInfo fileInfo,Func<FileStream,IObservable<T>> selector,
            FileMode fileMode = FileMode.OpenOrCreate, FileAccess fileAccess = FileAccess.Read, FileShare fileShare = FileShare.Read,int retry=5) 
            => Observable.Defer(async () => {
                if (fileMode==FileMode.OpenOrCreate && !Directory.Exists(fileInfo.DirectoryName)) {
                    Directory.CreateDirectory(fileInfo.DirectoryName!);
                }
                await using var fileStream = File.Open(fileInfo.FullName, fileMode, fileAccess, fileShare);
                return (await selector(fileStream)).Observe().AsObservable();
            }).RetryWithBackoff(retry);

        public static IObservable<DirectoryInfo> WhenDirectory(this DirectoryInfo directory,bool create=true) 
            => Observable.Defer(() => {
                if (!directory.Exists){
                    if (create) {
                        Directory.CreateDirectory(directory.FullName);
                        return directory.Observe();
                    }
                    return Observable.Empty<DirectoryInfo>();
                }
                return directory.Observe();
            });

        public static IObservable<FileInfo> OpenFile(this FileInfo fileInfo,FileMode fileMode=FileMode.Open,FileAccess fileAccess=FileAccess.Read,FileShare fileShare=FileShare.Read) 
            => Observable.Defer(() => Observable.Using(() => File.Open(fileInfo.FullName, fileMode, fileAccess, fileShare),
                    fileStream => {
                        fileStream.Dispose();
                        return fileInfo.Observe();
                    }))
                .RetryWithBackoff(strategy:_ => TimeSpan.FromMilliseconds(200));

    }
}