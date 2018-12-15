using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;

namespace DevExpress.XAF.Modules.Reactive.Extensions{
    public static class CommonExtensions{
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

        public static IObservable<(TDisposable frame,EventArgs args)> Disposed<TDisposable>(this IObservable<TDisposable> source) where TDisposable:IComponent{
            return source
                .SelectMany(item => {
                    return Observable.FromEventPattern<EventHandler, EventArgs>(h => item.Disposed += h, h => item.Disposed -= h);
                })
                .Select(pattern => pattern)
                .TransformPattern<EventArgs,TDisposable>();
        }

        public static IObservable<T> AsObservable<T>(this T self, IScheduler scheduler = null){
            scheduler = scheduler ?? Scheduler.Immediate;
            return Observable.Return(self, scheduler);
        }
    }
}