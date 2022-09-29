using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Xpand.Extensions.TaskExtensions;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static T Wait<T>(this IObservable<T> source, TimeSpan timeSpan) 
            => source.ToTask().Timeout(timeSpan).Result;
        public static IObservable<T> WaitUntilInactive<T>(this IObservable<T> source, TimeSpan timeSpan,int count =1) 
            =>timeSpan==TimeSpan.Zero?source: source.BufferUntilInactive(timeSpan).SelectMany(list => list.TakeLast(1).ToArray());
        
        public static IObservable<T> WaitUntilInactive<T>(this IObservable<T> source, int seconds, int count = 1)
            => source.WaitUntilInactive(TimeSpan.FromSeconds(seconds), count);
        
    }
}