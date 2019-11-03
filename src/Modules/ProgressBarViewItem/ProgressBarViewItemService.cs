using System;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.XAF.Modules.ProgressBarViewItem{
    static class ProgressBarViewItemService{
        internal static IObservable<TSource> TraceProgressBarViewItemModule<TSource>(this IObservable<TSource> source, string name = null,
            Action<string> traceAction = null,
            ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0){
            return source.Trace(name, ProgressBarViewItemModule.TraceSource, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        }

    }
}
