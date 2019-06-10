using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;

namespace Xpand.XAF.Modules.Reactive.Extensions{
    public static class CommonExtensions{
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
        public static IObservable<TSource> Tracer<TSource>(this IObservable<TSource> source,bool verbose=false){
            return source.Do(_ => {
                if (verbose){
                    Tracing.Tracer.LogVerboseText($"{_}");
                }
                else{
                    Tracing.Tracer.LogText($"{_}");
                }
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
            return FitsCore(view, viewType)&&FitsCore(view,nesting)&&objectType.IsAssignableFrom(view.ObjectTypeInfo.Type);
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

        public static IObservable<(TDisposable frame, EventArgs args)> WhenDisposed<TDisposable>(
            this TDisposable source) where TDisposable : IComponent{
            return Observable.Return(source).Disposed();
        }

        public static IObservable<Unit> ToUnit<T>(
            this IObservable<T> source){
            return source.Select(o => Unit.Default);
        }

        public static IObservable<T> TakeUntilDisposed<T>(this IObservable<T> source,IComponent component) {
            return source.TakeUntil(component.WhenDisposed());
        }

        public static IObservable<(TDisposable frame,EventArgs args)> Disposed<TDisposable>(this IObservable<TDisposable> source) where TDisposable:IComponent{
            return source
//                .SelectMany(item => Observable.StartAsync(async () =>await Observable.FromEventPattern<EventHandler, EventArgs>(h => item.Disposed += h, h => item.Disposed -= h)))
                .SelectMany(item => Observable.FromEventPattern<EventHandler, EventArgs>(h => item.Disposed += h, h => item.Disposed -= h).FirstAsync())
                .Select(pattern => pattern)
                .TransformPattern<EventArgs,TDisposable>();
        }

        public static IObservable<T> AsObservable<T>(this T self, IScheduler scheduler = null){
            scheduler = scheduler ?? Scheduler.Immediate;
            return Observable.Return(self, scheduler);
        }
    }
}