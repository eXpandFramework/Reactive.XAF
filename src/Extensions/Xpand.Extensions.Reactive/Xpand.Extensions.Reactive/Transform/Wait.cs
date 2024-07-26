using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Xpand.Extensions.TaskExtensions;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static T Wait<T>(this IObservable<T> source, TimeSpan timeSpan) 
            => source.ToTask().Timeout(timeSpan).Result;
        public static IObservable<T> WaitUntilInactive<T>(this IObservable<T> source, TimeSpan timeSpan,int count =1,IScheduler scheduler=null) 
            =>timeSpan==TimeSpan.Zero?source: source.BufferUntilInactive(timeSpan,scheduler:scheduler).SelectMany(list => list.TakeLast(count).ToArray());
        
        public static IObservable<T> WaitUntilInactive<T>(this IObservable<T> source, int seconds, int count = 1,IScheduler scheduler=null)
            => source.WaitUntilInactive(TimeSpan.FromSeconds(seconds), count,scheduler);
        
    }
}