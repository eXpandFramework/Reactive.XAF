using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.TestsLib.Common {
    public static class AssertExtensions {
        public static TimeSpan TimeoutInterval = (Debugger.IsAttached ? 120 : 15).Seconds();

        public static IObservable<Frame> AssertListViewHasObject<TObject>(this XafApplication application, Func<TObject,bool> matchObject=null,int count=0,[CallerMemberName]string caller="")  
            => application.WhenFrame(typeof(TObject),ViewType.ListView)
                .SelectUntilViewClosed(frame => frame.View.WhenControlsCreated(true).Take(1)
                    .SelectMany(view => view.ToListView().WhenObjects<TObject>()
                        .Where(value => matchObject?.Invoke(value)??true)
                        .SkipOrOriginal(count-1).Take(1)
                        .SelectMany(value => view.ToListView().SelectObject(value)).To(frame)
                        .Assert(_ => $"{typeof(TObject).Name}-{view.Id}",caller:caller)))
                .ReplayFirstTake();

        public static IObservable<TTabbedControl> AssertTabControl<TTabbedControl>(this XafApplication application,Type objectType=null,Func<DetailView,bool> match=null,Func<IModelTabbedGroup, bool> tabMatch=null,[CallerMemberName]string caller="") 
            => application.WhenTabControl<TTabbedControl>( objectType, match,tabMatch).Assert(objectType?.Name,caller:$"{caller} - {nameof(AssertTabControl)}");
        public static IObservable<TSource> Assert<TSource>(
            this IObservable<TSource> source, TimeSpan? timeout = null, [CallerMemberName] string caller = "") 
            => source.Assert(_ => "",timeout,caller);

        public static IObservable<TSource> Assert<TSource>(this IObservable<TSource> source, string message, TimeSpan? timeout = null,[CallerMemberName]string caller="")
            => source.Assert(_ => message,timeout,caller);

        
        public static TimeSpan? DelayOnContextInterval=250.Milliseconds();
        public static IObservable<TSource> Assert<TSource>(this IObservable<TSource> source,Func<TSource,string> messageFactory,TimeSpan? timeout=null,[CallerMemberName]string caller=""){
            var timeoutMessage = messageFactory.MessageFactory(caller);
            return source.Log(messageFactory, caller).ThrowIfEmpty(timeoutMessage).Timeout(timeout ?? TimeoutInterval, timeoutMessage)
                .DelayOnContext(DelayOnContextInterval)
                .ReplayFirstTake();
        }

        
        public static string MessageFactory<TSource>(this Func<TSource, string> messageFactory, string caller) => $"{caller}: {messageFactory(default)}";
    }
}