using System;
using System.Runtime.CompilerServices;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ModelMapper.Services{
    internal static class ModelMapperService{
        internal static IObservable<TSource> TraceModelMapper<TSource>(this IObservable<TSource> source, string name = null,
            Action<string> traceAction = null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0){
            return source.Trace(name, ModelMapperModule.TraceSource, traceAction, traceStrategy, memberName, sourceFilePath,
                sourceLineNumber);
        }
    }
}