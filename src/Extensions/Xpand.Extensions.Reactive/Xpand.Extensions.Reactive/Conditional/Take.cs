using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Conditional {
    public static partial class Conditional {
        public static IObservable<T> TakeUntilFinished<T,T2>(this IObservable<T> source, IObservable<T2> next)
            => source.TakeUntil(next.WhenFinished());
        
        public static IObservable<TSource> TakeWhileInclusive<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate) 
            => source.TakeUntil(source.SkipWhile(predicate).Skip(1));
        
        public static IObservable<T> TakeUntilInclusive<T>(this IObservable<T> source, Func<T, bool> predicate) 
            => source.Publish(co => co.TakeUntil(predicate).Merge(co.SkipUntil(predicate).Take(1)));
        
        public static IObservable<T> TakeUntilDisposed<T>(this IObservable<T> source, IComponent component, [CallerMemberName] string caller = "")
            => component != null ? source.TakeUntil(component.WhenDisposed(caller)) : source;
        public static IObservable<T> TakeUntilCompleted<T,T2>(this IObservable<T> source, IObservable<T2> next)
            => source.TakeUntil(next.WhenCompleted());    
        public static IObservable<T> TakeFirst<T>(this IObservable<T> source, Func<T, bool> predicate)
            => source.Where(predicate).Take(1);

        public static IConnectableObservable<T> TakeAndReplay<T>(this IObservable<T> source, int count)
            => source.Take(count).Replay(count);
        public static IObservable<T> TakeFirst<T>(this IObservable<T> source)
            => source.Take(1);
    }
}