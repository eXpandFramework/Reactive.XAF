using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using DevExpress.ExpressApp;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.RefreshView{
    public static class RefreshViewService{
        internal static IObservable<TSource> TraceRefreshView<TSource>(this IObservable<TSource> source, string name = null,
            Action<string> traceAction = null,ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0){

            return source.Trace(name, RefreshViewModule.TraceSource, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        }


        internal static IObservable<Unit> Connect(this  XafApplication application){
            return RefreshView(application)
                .ToUnit();
        }

        private static IObservable<Unit> RefreshView(XafApplication application){
            return application.WhenViewOnFrame().CombineLatest(application.ReactiveModulesModel().RefreshViewModel(),
                    (frame, model) => {
                        var synchronizationContext = SynchronizationContext.Current;
                        return model.Items
                            .Where(_ => _.View == frame.View.Model && _.Interval != TimeSpan.Zero)
                            .ToObservable()
                            .SelectMany(item => Observable.Interval(item.Interval)
                                .TakeUntil(frame.View.WhenClosing())
                                .ObserveOn(synchronizationContext)
                                .Select(l => {
                                    frame.View.RefreshDataSource();
                                    return frame.View;
                                })
                                );
                    }).Merge()
                .TraceRefreshView()
                .ToUnit()
                .Retry(application);
        }
    }
}