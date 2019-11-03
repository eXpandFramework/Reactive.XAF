using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.XAF.Modules.ModelMapper.Services{
    internal static class ModelMapperService{
        internal static IObservable<TSource> TraceModelMapper<TSource>(this IObservable<TSource> source,Func<TSource,TSource> traceSelector=null, string name = null,
            Action<string> traceAction = null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0){
            traceSelector = traceSelector ?? (_ => _);
            return source.SelectMany(_ => {
                return traceSelector(_).AsObservable().Trace(name, ModelMapperModule.TraceSource, traceAction, traceStrategy,
                    memberName, sourceFilePath,
                    sourceLineNumber).Select(__ => _);
            });
        }
    }
}