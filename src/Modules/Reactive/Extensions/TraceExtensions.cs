using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.XAF.Modules.Reactive.Extensions{
    public static class TraceExtensions{
        static TraceExtensions(){
            Utility.AddTraceSerialization<Frame>(_ =>
                $"{_.GetType().FullName} - ctx: {_.Context} - id: {_.View?.Id}");
            Utility.AddTraceSerialization<CollectionSourceBase>(_ =>
                $"{_.GetType().Name} - {_.ObjectTypeInfo.FullName}");
            Utility.AddTraceSerialization<ShowViewParameters>(_ =>
                $"{nameof(ShowViewParameters)} - {_.CreatedView.Id} - {_.Context}");
            Utility.AddTraceSerialization<ModuleBase>(_ => _.Name);
        }

        internal static IObservable<TSource> TraceRX<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
	        source.Trace(name, ReactiveModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

    }

    public class ReactiveTraceSource : TraceSource{
        public ReactiveTraceSource(string name) : base(name){
        }
    }
}