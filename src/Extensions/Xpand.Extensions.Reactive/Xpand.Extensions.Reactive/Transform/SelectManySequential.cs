using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Xpand.Extensions.Reactive.Transform{
    public static partial class Transform{
        public static IObservable<T2> SelectManySequential<T1, T2>(this IObservable<T1> source, Func<T1, IObservable<T2>> selector) 
            => source.Select(x => Observable.Defer(() => selector(x))).Concat();
        public static IObservable<T2> SelectManySequential<T1, T2>(this IObservable<T1> source, Func<T1, Task<T2>> selector) 
            => source.SelectManySequential(arg => selector(arg).ToObservable());
        
        public static IObservable<T2> SelectManySequential<T1, T2>(this IObservable<T1> source, Func<T1,int, IObservable<T2>> selector) 
            => source.Select((x,i) => Observable.Defer(() => selector(x,i))).Concat();
    }
}