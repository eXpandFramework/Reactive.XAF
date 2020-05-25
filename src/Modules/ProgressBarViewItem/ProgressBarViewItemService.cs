using System;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.XAF.Modules.ProgressBarViewItem{
    static class ProgressBarViewItemService{
        internal static IObservable<TSource> TraceProgressBarViewItemModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, ProgressBarViewItemModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);


    }
}
