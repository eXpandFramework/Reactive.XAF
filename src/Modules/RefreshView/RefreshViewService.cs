using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.RefreshView{
    public static class RefreshViewService{
        internal static IObservable<TSource> TraceRefreshView<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,Func<string> allMessageFactory = null,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, RefreshViewModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy,allMessageFactory, memberName,sourceFilePath,sourceLineNumber);

        internal static IObservable<Unit> Connect(this  ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.RefreshView());

        private static IObservable<Unit> RefreshView(this XafApplication application)
            => application.WhenFrame(frame => application.ReactiveModulesModel().RefreshViewModel()
                .SelectMany(model => model.Items.Where(item => item.View == frame.View.Model && item.Interval != TimeSpan.Zero).ToNowObservable()
                    .SelectMany(item => item.Interval.Interval().ObserveOnContext()
                        .Do(_ => frame.View?.RefreshDataSource()))),typeof(object)).ToUnit();
    }
}