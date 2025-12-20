using System;
using System.Reactive;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Channels{
    public static class RpcChannelSuppress {
        public static IObservable<TSource> Suppress<TSource, TSignal>(this IObservable<TSource> source, Func<TSource, TSignal> signalFactory) 
            => source.SelectMany(item => {
                var signal = signalFactory(item);
                return typeof(TSignal).MakeRequest()
                    .TryWith(signal, defaultValue: false)
                    .Where(shouldSuppress => !shouldSuppress)
                    .Select(_ => item);
            });

        public static IObservable<Unit> Suppress<T>(this Type key, Func<T, bool> predicate = null) 
            => Suppress<T, Type>(key, predicate);

        public static IObservable<Unit> Suppress<T, TKey>(this TKey key, Func<T, bool> predicate = null) where TKey : notnull
            => key.HandleRequest()
                .With<T, bool>(item => Observable.Return(predicate?.Invoke(item) ?? true)) ;

        
        public static IObservable<T> Suppress<T, TKey>(this IObservable<T> source, TKey key) where TKey : notnull
            => source.SelectMany(item => key.MakeRequest()
                .TryWith(item, defaultValue: false)
                .Where(ignore => !ignore)
                .Select(_ => item));
        
        public static SuppressionBuilder<TContext> Suppress<TContext>(this string key) => new(key);

        public readonly struct SuppressionBuilder<TContext>(string key) {
            public IObservable<Unit> Using(Func<TContext, bool> predicate) => key.SuppressWithContext( predicate);
        }
        

        public static IObservable<Unit> SuppressWithContext<TContext, TKey>(this TKey key, Func<TContext, bool> predicate) where TKey : notnull 
            => key.HandleRequest().With<TContext, bool>(context => Observable.Return(predicate(context)));

        public static IObservable<Unit> SuppressWithContext<TContext>(this string key, Func<TContext, bool> predicate)
            => key.SuppressWithContext<TContext, string>( predicate);
    }
}