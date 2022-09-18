using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using Xpand.Extensions.TaskExtensions;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static T Wait<T>(this IObservable<T> source, TimeSpan timeSpan) 
            => source.ToTask().Timeout(timeSpan).Result;
        public static IObservable<T> WaitUntilInactive<T>(this IObservable<T> source, TimeSpan timeSpan,int count =1) 
            => timeSpan == TimeSpan.Zero ? source : source.BufferUntilInactive(timeSpan).SelectMany(list => list.TakeLast(count));
        
        public static IObservable<T> WaitUntilInactive<T>(this IObservable<T> source, TimeSpan timeSpan,SynchronizationContext context,int count =1) 
            => timeSpan == TimeSpan.Zero ? source : source.BufferUntilInactive(timeSpan).ObserveOn(context!).SelectMany(list => list.TakeLast(count));

        public static IObservable<T> WaitUntilInactive<T>(this IObservable<T> source, int seconds, int count = 1)
            => source.WaitUntilInactive(TimeSpan.FromSeconds(seconds), count);
        public static IObservable<T> WaitUntilInactive<T>(this IObservable<T> source, int seconds,SynchronizationContext context, int count = 1)
            => source.WaitUntilInactive(TimeSpan.FromSeconds(seconds),context, count);
    }
}