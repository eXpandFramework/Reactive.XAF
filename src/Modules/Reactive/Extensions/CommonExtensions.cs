using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;

namespace Xpand.XAF.Modules.Reactive.Extensions{
    public static class CommonExtensions{
        public static IObservable<(T item, int length)> CountSubsequent<T>(this IObservable<T> source,Func<T,object> key){
            var eventTraceSubject = source.Publish().RefCount();
            return eventTraceSubject
                .GroupByUntil(key, _ => eventTraceSubject.DistinctUntilChanged(key))
                .SelectMany(ob => {
                    var r = ob.Replay();
                    r.Connect();
                    return r.IgnoreElements().Concat(default(T).AsObservable())
                        .Select(_ => r.ToEnumerable().ToArray());
                })
                .Select(_ => {
                    var item = _.First();
                    
                    return (item,_.Length);
                });
        }
        public static IObservable<IList<TOut>> WhenNotEmpty<TOut>(this IObservable<IList<TOut>> source){
            return source.Where(outs => outs.Any());
        }

        public static IObservable<TOut> Drain<TSource, TOut>(this IObservable<TSource> source,Func<TSource, IObservable<TOut>> selector){
            return Observable.Defer(() => {
                var queue = new BehaviorSubject<Unit>(new Unit());

                return source
                    .Zip(queue, (v, q) => v)
                    .SelectMany(v => selector(v)
                        .Do(_ => { }, () => queue.OnNext(new Unit()))
                    );
            });
        }

        public static IObservable<T> ObserveLatestOn<T>(this IObservable<T> source, IScheduler scheduler){
            return Observable.Create<T>(observer => {
                Notification<T> outsideNotification;
                var gate = new object();
                var active = false;
                var cancelable = new MultipleAssignmentDisposable();
                var disposable = source.Materialize().Subscribe(thisNotification => {
                    bool wasNotAlreadyActive;
                    lock (gate){
                        wasNotAlreadyActive = !active;
                        active = true;
                        outsideNotification = thisNotification;
                    }

                    if (wasNotAlreadyActive)
                        cancelable.Disposable = scheduler.Schedule(self => {
                            Notification<T> localNotification;
                            lock (gate){
                                localNotification = outsideNotification;
                                outsideNotification = null;
                            }

                            localNotification.Accept(observer);
                            bool hasPendingNotification;
                            lock (gate){
                                hasPendingNotification = active = outsideNotification != null;
                            }

                            if (hasPendingNotification) self();
                        });
                });
                return new CompositeDisposable(disposable, cancelable);
            });
        }

        public static IObservable<T> DoNotComplete<T>(this IObservable<T> source){
            return source.Concat(Observable.Never<T>());
        }

        public static IObservable<T> RetryWithBackoff<T>(this IObservable<T> source,int retryCount = 3,
            Func<int, TimeSpan> strategy = null,Func<Exception, bool> retryOnError = null,IScheduler scheduler = null){
            strategy = strategy ?? (n =>TimeSpan.FromSeconds(Math.Pow(n, 2))) ;
            var attempt = 0;
            retryOnError = retryOnError ?? (_ => true);
            return Observable.Defer(() => (++attempt == 1 ? source : source.DelaySubscription(strategy(attempt - 1), scheduler))
                    .Select(item => (true, item, (Exception)null))
                    .Catch<(bool, T, Exception), Exception>(e =>retryOnError(e)? Observable.Throw<(bool, T, Exception)>(e)
                        : Observable.Return<(bool, T, Exception)>((false, default, e))))
                .Retry(retryCount)
                .SelectMany(t => t.Item1
                    ? Observable.Return(t.Item2)
                    : Observable.Throw<T>(t.Item3));
        }

        public static IObservable<T> DelaySubscription<T>(this IObservable<T> source,
            TimeSpan delay, IScheduler scheduler = null){
            if (scheduler == null) return Observable.Timer(delay).SelectMany(_ => source);
            return Observable.Timer(delay, scheduler).SelectMany(_ => source);
        }

        public static IObservable<T> SubscribeReplay<T>(this IObservable<T> source,int bufferSize=0){
            var replay = bufferSize>0?source.Replay(bufferSize):source.Replay();
            replay.Connect();
            return replay;
        }

        public static ConfiguredTaskAwaitable<T> ToTaskWithoutConfigureAwait<T>(this IObservable<T> source){
            return source.ToTask().ConfigureAwait(false);
        }

        public static T Wait<T>(this IObservable<T> source, TimeSpan timeSpan){
            return source.Timeout(timeSpan).Wait();
        }
        public static IObservable<TC> MergeOrCombineLatest<TA, TB, TC>(
            this IObservable<TA> a,
            IObservable<TB> b,
            Func<TA, TC> aResultSelector, // When A starts before B
            Func<TB, TC> bResultSelector, // When B starts before A
            Func<TA, TB, TC> bothResultSelector) // When both A and B have started
        {
            return
                a.Publish(aa =>
                    b.Publish(bb =>
                        aa.CombineLatest(bb, bothResultSelector).Publish(xs =>
                            aa
                                .Select(aResultSelector)
                                .Merge(bb.Select(bResultSelector))
                                .TakeUntil(xs)
                                .SkipLast(1)
                                .Merge(xs))));
        }
        public static IObservable<TSource> Tracer<TSource>(this IObservable<TSource> source,string text=null,bool verbose=false){
            if (!string.IsNullOrEmpty(text)){
                text = $"{text}:";
            }
            return source.Do(_ => {
                if (verbose){
                    Tracing.Tracer.LogVerboseText($"{text}{_}");
                }
                else{
                    Tracing.Tracer.LogText($"{text}{_}");
                }
            });
        }

        public static IObservable<TSource> WhenDefault<TSource>(this IObservable<TSource> source){
            return source.Where(_ => {
                var def = default(TSource);
                return def != null && def.Equals(_);
            });
        }

        public static IObservable<TSource> WhenNotDefault<TSource>(this IObservable<TSource> source){
            return source.Select(_ => (object)_).Where(o => o!=null)
                .Select(o => (TSource)o)
                .Where(_ =>!_.Equals(default(TSource)));
        }

        public static IObservable<(TSource previous,TSource current)> CombineWithPrevious<TSource>(this IObservable<TSource> source){
            return source
                .Scan((previous:default(TSource), current:default(TSource)),(_, current) => (_.current, current))
                .Select(t => (t.previous, t.current));
        }

        internal static bool Fits(this View view,ViewType viewType=ViewType.Any,Nesting nesting=Nesting.Any,Type objectType=null) {
            objectType = objectType ?? typeof(object);
            return FitsCore(view, viewType)&&FitsCore(view,nesting)&&objectType.IsAssignableFrom(view.ObjectTypeInfo?.Type);
        }

        private static bool FitsCore(View view, ViewType viewType){
            if (view == null)
                return false;
            if (viewType == ViewType.ListView)
                return view is ListView;
            if (viewType == ViewType.DetailView)
                return view is DetailView;
            if (viewType == ViewType.DashboardView)
                return view is DashboardView;
            return true;
        }

        private static bool FitsCore(View view, Nesting nesting) {
            return nesting == Nesting.Nested ? !view.IsRoot : nesting != Nesting.Root || view.IsRoot;
        }

        public static IConnectableObservable<T> BufferUntilSubscribed<T>(this IObservable<T> source) {
            return new BufferUntilSubscribedObservable<T>(source, Scheduler.Immediate);
        }

        public static IObservable<(T sender, TEventArgs e)> TransformPattern<TEventArgs,T>(this IObservable<EventPattern<TEventArgs>> source) where TEventArgs:EventArgs{
            return source.Select(pattern => ((T) pattern.Sender, pattern.EventArgs));
        }

        public static IObservable<(TDisposable component, EventArgs args)> WhenDisposed<TDisposable>(
            this TDisposable source) where TDisposable : IComponent{
            Guard.ArgumentNotNull(source,nameof(source));
            return Observable.Return(source).Disposed();
        }

        public static IObservable<TValue> MergeWith<TSource, TValue>(this IObservable<TSource> source, TValue value,IScheduler scheduler=null){
            scheduler = scheduler ?? CurrentThreadScheduler.Instance;
            return source.Merge(Observable.Return(default(TSource),scheduler)).Select(_ => value);
        }

        public static IObservable<TValue> To<TSource,TValue>(this IObservable<TSource> source,TValue value){
            return source.Select(o => value);
        }

        public static IObservable<T> To<T>(this IObservable<object> source){
            return source.Select(o => default(T)).WhenNotDefault();
        }

        public static IObservable<Unit> ToUnit<T>(this IObservable<T> source){
            return source.Select(o => Unit.Default);
        }

        public static IObservable<T> TakeUntilDisposed<T>(this IObservable<T> source,IComponent component) {
            return source.TakeUntil(component.WhenDisposed());
        }

        public static IObservable<(TDisposable component,EventArgs args)> Disposed<TDisposable>(this IObservable<TDisposable> source) where TDisposable:IComponent{
            return source
                .SelectMany(item => Observable.FromEventPattern<EventHandler, EventArgs>(h => item.Disposed += h, h => item.Disposed -= h))
                .Select(pattern => pattern)
                .TransformPattern<EventArgs,TDisposable>();
        }

        public static IObservable<T> AsObservable<T>(this T self, IScheduler scheduler = null){
            scheduler = scheduler ?? Scheduler.Immediate;
            return Observable.Return(self, scheduler);
        }
    }
}