using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;

namespace Xpand.XAF.Modules.Reactive.Extensions{
    public static class TraceExtensions{
        static TraceExtensions(){
            Utility.AddTraceSerialization<Frame>(frame =>
                $"{frame.GetType().FullName} - ctx: {frame.Context} - id: {frame.View?.Id}");
            Utility.AddTraceSerialization<CollectionSourceBase>(collectionSourceBase =>
                $"{collectionSourceBase.GetType().Name} - {collectionSourceBase.ObjectTypeInfo.FullName}");
            Utility.AddTraceSerialization<ShowViewParameters>(parameters =>
                $"{nameof(ShowViewParameters)} - {parameters.CreatedView.Id} - {parameters.Context}");
            Utility.AddTraceSerialization<ModuleBase>(moduleBase => moduleBase.Name);
        }

        internal static IObservable<TSource> TraceRX<TSource>(this IObservable<TSource> source,
            Func<TSource, string> messageFactory = null, string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception, string> errorMessageFactory = null,
            ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,
            [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
            => source.Trace(name, ReactiveModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        
        internal static IObservable<TSource> TraceErrorRX<TSource>(this IObservable<TSource> source,
            Func<TSource, string> messageFactory = null, string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception, string> errorMessageFactory = null,
            ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,
            [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
            => source.Trace(name, ReactiveModule.TraceSource,messageFactory,errorMessageFactory, traceAction,ObservableTraceStrategy.OnError, memberName,sourceFilePath,sourceLineNumber);
    }

    public class ReactiveTraceSource(string name) : TraceSource(name) {
        static ReactiveTraceSource() => Trace.UseGlobalLock = false;

        public override string ToString() => Name;
    }
}